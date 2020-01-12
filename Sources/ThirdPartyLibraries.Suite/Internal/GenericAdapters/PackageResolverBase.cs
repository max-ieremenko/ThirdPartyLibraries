using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.GenericAdapters
{
    internal abstract class PackageResolverBase : IPackageResolver
    {
        private readonly ConcurrentDictionary<LibraryId, byte[]> _packageContentById = new ConcurrentDictionary<LibraryId, byte[]>();

        [Dependency]
        public ILicenseResolver LicenseResolver { get; set; }

        [Dependency]
        public IStorage Storage { get; set; }

        protected abstract bool DownloadPackageIntoRepository { get; }

        protected abstract string RepositoryPackageFileName { get; }

        public async ValueTask<bool> DownloadAsync(LibraryId id, CancellationToken token)
        {
            var index = await Storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(id, token);
            var isNew = false;

            if (index == null)
            {
                isNew = true;
                index = new LibraryIndexJson();
                await CreateNewAsync(id, index, token);
            }
            else
            {
                var exists = await Storage.LibraryFileExistsAsync(id, RepositoryPackageFileName, token);
                if (!exists && DownloadPackageIntoRepository)
                {
                    await GetPackageContentAsync(id, token);
                }
            }

            _packageContentById.TryRemove(id, out _);
            var indexSnapshot = isNew ? default : new LibraryIndexJsonSnapshot(index);

            if (index.License.Code.IsNullOrEmpty())
            {
                foreach (var license in index.Licenses)
                {
                    await RefreshLicenseAsync(id, license, token);
                 
                    if (license.Subject.EqualsIgnoreCase(PackageLicense.SubjectPackage))
                    {
                        await CheckPackageLicenseCodeMatchUrlAsync(id, license, token);
                        if (license.Description.IsNullOrEmpty())
                        {
                            index.License.Code = license.Code;
                        }
                    }
                }
            }

            if (isNew || indexSnapshot.HasChanges(index))
            {
                await Storage.WriteLibraryIndexJsonAsync(id, index, token);
            }

            return isNew;
        }

        protected abstract Task CreateNewAsync(LibraryId id, LibraryIndexJson index, CancellationToken token);

        protected abstract Task<byte[]> DownloadPackageContentAsync(LibraryId id, CancellationToken token);

        protected abstract Task<byte[]> GetPackageFileContentAsync(LibraryId id, string fileName, CancellationToken token);

        protected async Task<byte[]> GetPackageContentAsync(LibraryId id, CancellationToken token)
        {
            using (var stream = await Storage.OpenLibraryFileReadAsync(id, RepositoryPackageFileName, token))
            {
                if (stream != null)
                {
                    return await stream.ToArrayAsync(token);
                }
            }

            if (!_packageContentById.TryGetValue(id, out var content))
            {
                content = await DownloadPackageContentAsync(id, token);
                if (content == null)
                {
                    throw new InvalidOperationException("Package {0} {1} not found on {2}.".FormatWith(id.Name, id.Version, id.SourceCode));
                }

                _packageContentById.TryAdd(id, content);

                if (DownloadPackageIntoRepository)
                {
                    await Storage.WriteLibraryFileAsync(id, RepositoryPackageFileName, content, token);
                }
            }

            return content;
        }

        protected async Task<LibraryLicense> ResolvePackageLicenseAsync(LibraryId id, string licenseType, string licenseValue, string licenseUrl, CancellationToken token)
        {
            var license = new LibraryLicense { Subject = PackageLicense.SubjectPackage };
            var hasFileName = false;

            if (licenseType.EqualsIgnoreCase("file"))
            {
                var content = await GetPackageFileContentAsync(id, licenseValue, token);
                await Storage.WriteLibraryFileAsync(id, PackageLicense.SubjectPackage + "-" + licenseValue, content ?? Array.Empty<byte>(), token);
                hasFileName = true;
            }
            else if (licenseType.EqualsIgnoreCase("expression"))
            {
                license.Code = licenseValue;
            }

            if (!licenseUrl.IsNullOrEmpty())
            {
                license.HRef = licenseUrl;
            }

            if (!hasFileName)
            {
                var fileNames = new[] { "LICENSE.md", "LICENSE.txt", "LICENSE", "LICENSE.rtf" };
                foreach (var fileName in fileNames)
                {
                    var content = await GetPackageFileContentAsync(id, fileName, token);
                    if (content != null)
                    {
                        await Storage.WriteLibraryFileAsync(id, PackageLicense.SubjectPackage + "-" + fileName, content, token);
                    }
                }
            }

            return license;
        }

        protected async Task<LibraryLicense> ResolveUrlLicenseAsync(LibraryId id, string url, string subject, CancellationToken token)
        {
            var license = new LibraryLicense
            {
                Subject = subject,
                HRef = url
            };

            var info = await LicenseResolver.ResolveByUrlAsync(url, token);

            license.Code = info?.Code;

            if (info?.FileContent != null)
            {
                await Storage.WriteLibraryFileAsync(id, subject + "-" + info.FileName, info.FileContent, token);
            }

            return license;
        }

        private async Task CheckPackageLicenseCodeMatchUrlAsync(LibraryId id, LibraryLicense license, CancellationToken token)
        {
            if (license.Code.IsNullOrEmpty() || license.HRef.IsNullOrEmpty())
            {
                return;
            }

            var test = await ResolveUrlLicenseAsync(id, license.HRef, license.Subject, token);

            if (test.Code.IsNullOrEmpty())
            {
                license.Description = "License code {0} should be verified on {1}".FormatWith(license.Code, license.HRef);
            }
            else if (!test.Code.EqualsIgnoreCase(license.Code))
            {
                license.Description = "License code {0} does not match code {1} from {2}".FormatWith(license.Code, test.Code, license.HRef);
            }
            else
            {
                license.Description = null;
            }
        }

        private async Task RefreshLicenseAsync(LibraryId id, LibraryLicense license, CancellationToken token)
        {
            if (!license.Code.IsNullOrEmpty() || license.HRef.IsNullOrEmpty())
            {
                return;
            }

            var test = await ResolveUrlLicenseAsync(id, license.HRef, license.Subject, token);
            if (test.Code.IsNullOrEmpty())
            {
                license.Description = "License should be verified on {0}".FormatWith(license.HRef);
            }
            else
            {
                license.Description = null;
                license.Code = test.Code;
            }
        }

        private readonly struct LibraryIndexJsonSnapshot
        {
            private readonly string _licenseCode;
            private readonly IDictionary<string, string> _licenseBySubject;

            public LibraryIndexJsonSnapshot(LibraryIndexJson index)
            {
                _licenseCode = index.License.Code;
                _licenseBySubject = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var license in index.Licenses)
                {
                    _licenseBySubject.Add(license.Subject, GetLicenseText(license));
                }
            }

            public bool HasChanges(LibraryIndexJson index)
            {
                if (!string.Equals(_licenseCode, index.License.Code, StringComparison.Ordinal))
                {
                    return true;
                }

                foreach (var license in index.Licenses)
                {
                    if (!_licenseBySubject.TryGetValue(license.Subject, out var text)
                        || !text.Equals(GetLicenseText(license), StringComparison.Ordinal))
                    {
                        return true;
                    }

                    _licenseBySubject.Remove(license.Subject);
                }

                return _licenseBySubject.Count == 0;
            }

            private static string GetLicenseText(LibraryLicense license)
            {
                return "{0}:{1}".FormatWith(license.Code, license.Description);
            }
        }
    }
}
