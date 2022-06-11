using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.GenericAdapters
{
    internal abstract class PackageResolverBase : IPackageResolver
    {
        private readonly ConcurrentDictionary<LibraryId, byte[]> _packageContentById = new ConcurrentDictionary<LibraryId, byte[]>();

        protected PackageResolverBase(ILicenseResolver licenseResolver, IStorage storage)
        {
            LicenseResolver = licenseResolver;
            Storage = storage;
        }

        public ILicenseResolver LicenseResolver { get; }

        public IStorage Storage { get; }

        protected abstract bool DownloadPackageIntoRepository { get; }

        protected abstract string RepositoryPackageFileName { get; }

        public async ValueTask<bool> DownloadAsync(LibraryId id, CancellationToken token)
        {
            var index = await Storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(id, token).ConfigureAwait(false);
            var isNew = false;

            if (index == null)
            {
                isNew = true;
                index = new LibraryIndexJson();
                await CreateNewAsync(id, index, token).ConfigureAwait(false);
            }
            else
            {
                var exists = await Storage.LibraryFileExistsAsync(id, RepositoryPackageFileName, token).ConfigureAwait(false);
                if (!exists && DownloadPackageIntoRepository)
                {
                    await GetPackageContentAsync(id, token).ConfigureAwait(false);
                }
            }

            _packageContentById.TryRemove(id, out _);
            var indexSnapshot = isNew ? default : new LibraryIndexJsonSnapshot(index);

            if (index.License.Code.IsNullOrEmpty())
            {
                foreach (var license in index.Licenses)
                {
                    await RefreshLicenseAsync(id, license, token).ConfigureAwait(false);
                 
                    if (license.Subject.EqualsIgnoreCase(PackageLicense.SubjectPackage))
                    {
                        await CheckPackageLicenseCodeMatchUrlAsync(id, license, token).ConfigureAwait(false);
                        if (license.Description.IsNullOrEmpty())
                        {
                            index.License.Code = license.Code;
                        }
                    }
                }
            }

            if (isNew || indexSnapshot.HasChanges(index))
            {
                await Storage.WriteLibraryIndexJsonAsync(id, index, token).ConfigureAwait(false);
            }

            return isNew;
        }

        protected abstract Task CreateNewAsync(LibraryId id, LibraryIndexJson index, CancellationToken token);

        protected abstract Task<byte[]> DownloadPackageContentAsync(LibraryId id, CancellationToken token);

        protected abstract Task<byte[]> GetPackageFileContentAsync(byte[] package, string fileName, CancellationToken token);

        protected abstract string[] FindPackageFiles(byte[] package, string searchPattern);

        protected async Task<byte[]> GetPackageContentAsync(LibraryId id, CancellationToken token)
        {
            using (var stream = await Storage.OpenLibraryFileReadAsync(id, RepositoryPackageFileName, token).ConfigureAwait(false))
            {
                if (stream != null)
                {
                    return await stream.ToArrayAsync(token).ConfigureAwait(false);
                }
            }

            if (!_packageContentById.TryGetValue(id, out var content))
            {
                content = await DownloadPackageContentAsync(id, token).ConfigureAwait(false);
                if (content == null)
                {
                    throw new InvalidOperationException("Package {0} {1} not found on {2}.".FormatWith(id.Name, id.Version, id.SourceCode));
                }

                _packageContentById.TryAdd(id, content);

                if (DownloadPackageIntoRepository)
                {
                    await Storage.WriteLibraryFileAsync(id, RepositoryPackageFileName, content, token).ConfigureAwait(false);
                }
            }

            return content;
        }

        protected async Task<LibraryLicense> ResolvePackageLicenseAsync(LibraryId id, string licenseType, string licenseValue, string licenseUrl, CancellationToken token)
        {
            var license = new LibraryLicense { Subject = PackageLicense.SubjectPackage };
            string licenseFileName = null;

            if (licenseType.EqualsIgnoreCase("file"))
            {
                licenseFileName = licenseValue;
            }
            else if (licenseType.EqualsIgnoreCase("expression"))
            {
                license.Code = licenseValue;
            }

            if (!licenseUrl.IsNullOrEmpty())
            {
                license.HRef = licenseUrl;
            }

            await CopyPackageLicenseFileAsync(id, licenseFileName, token).ConfigureAwait(false);

            return license;
        }

        protected async Task<LibraryLicense> ResolveUrlLicenseAsync(LibraryId id, string url, string subject, CancellationToken token)
        {
            var license = new LibraryLicense
            {
                Subject = subject,
                HRef = url
            };

            var info = await LicenseResolver.ResolveByUrlAsync(url, token).ConfigureAwait(false);

            license.Code = info?.Code;

            if (info?.FileContent != null)
            {
                await Storage.WriteLibraryFileAsync(id, PackageLicense.GetLicenseFileName(subject, info.FileName), info.FileContent, token).ConfigureAwait(false);
            }

            return license;
        }

        private async Task CheckPackageLicenseCodeMatchUrlAsync(LibraryId id, LibraryLicense license, CancellationToken token)
        {
            if (license.Code.IsNullOrEmpty() || license.HRef.IsNullOrEmpty())
            {
                return;
            }

            var test = await ResolveUrlLicenseAsync(id, license.HRef, license.Subject, token).ConfigureAwait(false);

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

            var test = await ResolveUrlLicenseAsync(id, license.HRef, license.Subject, token).ConfigureAwait(false);
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

        private async Task CopyPackageLicenseFileAsync(LibraryId id, string licenseFileName, CancellationToken token)
        {
            var package = await GetPackageContentAsync(id, token).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(licenseFileName))
            {
                var content = await GetPackageFileContentAsync(package, licenseFileName, token).ConfigureAwait(false);
                if (content != null)
                {
                    await Storage.WriteLibraryFileAsync(id, PackageLicense.GetLicenseFileName(PackageLicense.SubjectPackage, licenseFileName), content, token).ConfigureAwait(false);
                    return;
                }
            }

            foreach (var fileName in PackageLicense.StaticLicenseFileNames)
            {
                var content = await GetPackageFileContentAsync(package, fileName, token).ConfigureAwait(false);
                if (content != null)
                {
                    await Storage.WriteLibraryFileAsync(id, PackageLicense.GetLicenseFileName(PackageLicense.SubjectPackage, fileName), content, token).ConfigureAwait(false);
                    return;
                }
            }

            var files = FindPackageFiles(package, PackageLicense.DefaultLicenseFilePattern);
            if (files.Length == 1)
            {
                var content = await GetPackageFileContentAsync(package, files[0], token).ConfigureAwait(false);
                await Storage.WriteLibraryFileAsync(id, PackageLicense.GetLicenseFileName(PackageLicense.SubjectPackage, files[0]), content, token).ConfigureAwait(false);
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
