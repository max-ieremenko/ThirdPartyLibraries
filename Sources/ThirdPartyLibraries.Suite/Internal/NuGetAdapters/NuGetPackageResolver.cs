using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal sealed class NuGetPackageResolver : IPackageResolver
    {
        public const string LicenseSubjectPackage = "package";
        public const string LicenseSubjectProject = "project";
        public const string LicenseSubjectRepository = "repository";

        private const string DeprecateLicenseUrl = "https://aka.ms/deprecateLicenseUrl";

        [Dependency]
        public INuGetApi NuGetApi { get; set; }

        [Dependency]
        public NuGetConfiguration Configuration { get; set; }

        [Dependency]
        public ILicenseResolver LicenseResolver { get; set; }

        public async Task<Package> DownloadAsync(LibraryId id, CancellationToken token)
        {
            var (specContent, spec) = await LoadSpecAsync(id, token);
            var (license, licenseFile) = await ResolvePackageLicenseAsync(spec, token);

            var result = NuGetConstants.CreatePackage(spec, license.Code, null);
            result.Licenses.Add(license);
            result.Attachments.Add(new PackageAttachment(NuGetConstants.RepositorySpecFileName, specContent));

            if (licenseFile != null)
            {
                licenseFile.Name = LicenseSubjectPackage + "-" + licenseFile.Name;
                result.Attachments.Add(licenseFile);
            }

            if (spec.Repository != null)
            {
                (license, licenseFile) = await ResolveUrlLicenseAsync(spec.Repository.Url, LicenseSubjectRepository, token);
                result.Licenses.Add(license);
                if (licenseFile != null)
                {
                    licenseFile.Name = LicenseSubjectRepository + "-" + licenseFile.Name;
                    result.Attachments.Add(licenseFile);
                }
            }
            
            if (!string.IsNullOrEmpty(spec.ProjectUrl))
            {
                (license, licenseFile) = await ResolveUrlLicenseAsync(spec.ProjectUrl, LicenseSubjectProject, token);
                result.Licenses.Add(license);
                if (licenseFile != null)
                {
                    licenseFile.Name = LicenseSubjectProject + "-" + licenseFile.Name;
                    result.Attachments.Add(licenseFile);
                }
            }

            return result;
        }

        public async Task TryUpdateLicenseAsync(Package package, CancellationToken token)
        {
            package.AssertNotNull(nameof(package));
            if (!package.SourceCode.EqualsIgnoreCase(PackageSources.NuGet))
            {
                throw new ArgumentOutOfRangeException(nameof(package));
            }

            foreach (var license in package.Licenses)
            {
                if (license.Code.IsNullOrEmpty() && !license.HRef.IsNullOrEmpty())
                {
                    await ValidateLicenseUrl(license, token);
                }

                if (license.Subject.EqualsIgnoreCase(LicenseSubjectPackage))
                {
                    package.LicenseCode = license.Code;
                }
            }
        }

        private async Task<(PackageLicense License, PackageAttachment Content)> ResolvePackageLicenseAsync(NuGetSpec spec, CancellationToken token)
        {
            var license = new PackageLicense { Subject = LicenseSubjectPackage };
            PackageAttachment attachment = null;

            if (spec.License != null)
            {
                if (spec.License.Type.EqualsIgnoreCase("file"))
                {
                    attachment = new PackageAttachment
                    {
                        Name = spec.License.Value,
                        Content = await NuGetApi.LoadFileContentAsync(new NuGetPackageId(spec.Id, spec.Version), spec.License.Value, Configuration.AllowToUseLocalCache, token)
                    };
                }
                else if (spec.License.Type.EqualsIgnoreCase("expression"))
                {
                    license.Code = spec.License.Value;
                }
                else
                {
                    throw new NotSupportedException("<license type=\"{0}\">{1}</license>".FormatWith(spec.License.Type, spec.License.Value));
                }
            }

            if (!string.IsNullOrEmpty(spec.LicenseUrl) && !spec.LicenseUrl.Equals(DeprecateLicenseUrl))
            {
                license.HRef = spec.LicenseUrl;
                await ValidateLicenseUrl(license, token);
            }

            if (attachment == null)
            {
                var file = await NuGetApi.TryFindLicenseFileAsync(new NuGetPackageId(spec.Id, spec.Version), Configuration.AllowToUseLocalCache, token);
                if (file.HasValue)
                {
                    attachment = new PackageAttachment(file.Value.Name, file.Value.Content);
                }
            }

            return (license, attachment);
        }

        private async Task ValidateLicenseUrl(PackageLicense license, CancellationToken token)
        {
            var info = await LicenseResolver.ResolveByUrlAsync(license.HRef, token);
            UpdateLicenseHints(license, info);
        }

        private async Task<(PackageLicense License, PackageAttachment Content)> ResolveUrlLicenseAsync(string url, string subject, CancellationToken token)
        {
            var license = new PackageLicense
            {
                Subject = subject,
                HRef = url
            };

            var info = await LicenseResolver.ResolveByUrlAsync(url, token);
            UpdateLicenseHints(license, info);

            license.HRef = info?.FileHRef ?? url;

            PackageAttachment content = null;
            if (info?.FileContent != null)
            {
                content = new PackageAttachment(info.FileName, info.FileContent);
            }

            return (license, content);
        }

        private async Task<(byte[] Content, NuGetSpec spec)> LoadSpecAsync(LibraryId id, CancellationToken token)
        {
            var content = await NuGetApi.LoadSpecAsync(new NuGetPackageId(id.Name, id.Version), Configuration.AllowToUseLocalCache, token);

            if (content == null)
            {
                throw new InvalidOperationException("Package {0} {1} not found on www.nuget.org.".FormatWith(id.Name, id.Version));
            }

            using (var stream = new MemoryStream(content))
            {
                return (content, NuGetApi.ParseSpec(stream));
            }
        }

        private void UpdateLicenseHints(PackageLicense license, LicenseInfo info)
        {
            if (info == null)
            {
                if (license.Code.IsNullOrEmpty())
                {
                    license.CodeDescription = "License should be verified on {0}".FormatWith(license.HRef);
                    return;
                }

                license.CodeDescription = "License {0} should be verified on {1}".FormatWith(license.Code, license.HRef);
                license.Code = null;
                return;
            }

            if (info.Code.IsNullOrEmpty())
            {
                if (license.Code.IsNullOrEmpty())
                {
                    license.CodeDescription = "License should be verified on {0}".FormatWith(license.HRef);
                    return;
                }

                license.CodeDescription = "License {0} should be verified on {1}".FormatWith(license.Code, license.HRef);
                license.Code = null;
                return;
            }

            if (info.Code.EqualsIgnoreCase(license.Code))
            {
                return;
            }

            if (license.Code.IsNullOrEmpty())
            {
                license.Code = info.Code;
                return;
            }

            license.CodeDescription = "Package license {0} does not match {1} from {2}".FormatWith(license.Code, info.Code, license.HRef);
            license.Code = null;
        }
    }
}
