using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Npm;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters
{
    internal sealed class NpmPackageResolver : IPackageResolver
    {
        public const string LicenseSubjectPackage = "package";
        public const string LicenseSubjectHomePage = "homepage";
        public const string LicenseSubjectRepository = "repository";

        [Dependency]
        public INpmApi NpmApi { get; set; }

        [Dependency]
        public NpmConfiguration Configuration { get; set; }

        [Dependency]
        public ILicenseResolver LicenseResolver { get; set; }

        public async Task<Package> DownloadAsync(LibraryId id, CancellationToken token)
        {
            var file = await DownloadPackageAsync(new NpmPackageId(id.Name, id.Version), token);

            var jsonContent = await NpmApi.ExtractPackageJsonAsync(file.Content, token);
            var json = NpmApi.ParsePackageJson(jsonContent);

            var (license, licenseFile) = await ResolvePackageLicenseAsync(json, file.Content, token);

            var result = NpmConstants.CreatePackage(json, license.Code, null);
            result.Licenses.Add(license);
            result.Attachments.Add(new PackageAttachment(NpmConstants.RepositoryPackageJsonFileName, jsonContent));

            if (licenseFile != null)
            {
                licenseFile.Name = LicenseSubjectPackage + "-" + licenseFile.Name;
                result.Attachments.Add(licenseFile);
            }

            if (json.Repository != null)
            {
                (license, licenseFile) = await ResolveUrlLicenseAsync(json.Repository.Url, LicenseSubjectRepository, token);
                result.Licenses.Add(license);
                if (licenseFile != null)
                {
                    licenseFile.Name = LicenseSubjectRepository + "-" + licenseFile.Name;
                    result.Attachments.Add(licenseFile);
                }
            }

            if (!json.HomePage.IsNullOrEmpty())
            {
                (license, licenseFile) = await ResolveUrlLicenseAsync(json.HomePage, LicenseSubjectHomePage, token);
                result.Licenses.Add(license);
                if (licenseFile != null)
                {
                    licenseFile.Name = LicenseSubjectHomePage + "-" + licenseFile.Name;
                    result.Attachments.Add(licenseFile);
                }
            }

            return result;
        }

        public async Task TryUpdateLicenseAsync(Package package, CancellationToken token)
        {
            package.AssertNotNull(nameof(package));
            if (!package.SourceCode.EqualsIgnoreCase(PackageSources.Npm))
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

        private async Task<(PackageLicense License, PackageAttachment Content)> ResolvePackageLicenseAsync(PackageJson json, byte[] packageContent, CancellationToken token)
        {
            var license = new PackageLicense { Subject = LicenseSubjectPackage };
            PackageAttachment attachment = null;

            if (json.License != null)
            {
                if (json.License.Type.EqualsIgnoreCase("file"))
                {
                    attachment = new PackageAttachment
                    {
                        Name = json.License.Value,
                        Content = await NpmApi.LoadFileContentAsync(packageContent, json.License.Value, token)
                    };
                }
                else if (json.License.Type.EqualsIgnoreCase("expression"))
                {
                    license.Code = json.License.Value;
                }
            }

            if (attachment == null)
            {
                var file = await NpmApi.TryFindLicenseFileAsync(packageContent, token);
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

        private async Task<NpmPackageFile> DownloadPackageAsync(NpmPackageId id, CancellationToken token)
        {
            var file = await NpmApi.DownloadPackageAsync(new NpmPackageId(id.Name, id.Version), token);
            
            if (file == null)
            {
                throw new InvalidOperationException("Package {0} {1} not found on npmjs.com.".FormatWith(id.Name, id.Version));
            }

            return file.Value;
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
