using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.CustomAdapters
{
    internal sealed class CustomPackageRepositoryAdapter : IPackageRepositoryAdapter
    {
        private const string ThirdPartyNoticesFileName = "third-party-notices.txt";

        [Dependency]
        public IStorage Storage { get; set; }

        public async Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token)
        {
            var index = await Storage.ReadLibraryIndexJsonAsync<CustomLibraryIndexJson>(id, CancellationToken.None);
            return new Package
            {
                SourceCode = PackageSources.Custom,
                Name = index.Name,
                Version = index.Version,
                LicenseCode = index.LicenseCode,
                UsedBy = index.UsedBy.Select(i => i.Name).ToArray(),
                ApprovalStatus = PackageApprovalStatus.Approved
            };
        }

        public Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token)
        {
            throw new NotSupportedException();
        }

        public async Task<PackageReadMe> UpdatePackageReadMeAsync(LibraryId id, CancellationToken token)
        {
            using (var stream = await Storage.OpenLibraryFileReadAsync(id, ThirdPartyNoticesFileName, CancellationToken.None))
            {
                if (stream == null)
                {
                    await Storage.WriteLibraryFileAsync(id, ThirdPartyNoticesFileName, Array.Empty<byte>(), token);
                }
            }

            var index = await Storage.ReadLibraryIndexJsonAsync<CustomLibraryIndexJson>(id, CancellationToken.None);
            return new PackageReadMe
            {
                SourceCode = PackageSources.Custom,
                Name = index.Name,
                Version = index.Version,
                LicenseCode = index.LicenseCode,
                ApprovalStatus = PackageApprovalStatus.Approved,
                UsedBy = PackageReadMe.BuildUsedBy(index.UsedBy)
            };
        }

        public async Task<PackageNotices> LoadPackageNoticesAsync(LibraryId id, CancellationToken token)
        {
            var index = await Storage.ReadLibraryIndexJsonAsync<CustomLibraryIndexJson>(id, CancellationToken.None);
            return new PackageNotices
            {
                Name = index.Name,
                Version = index.Version,
                LicenseCode = index.LicenseCode,
                Copyright = index.Copyright,
                HRef = index.HRef,
                Author = index.Author,
                UsedBy = index.UsedBy.Select(i => new PackageNoticesApplication(i.Name, i.InternalOnly)).ToArray(),
                ThirdPartyNotices = await LoadThirdPartyNoticesAsync(id, token)
            };
        }

        public ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token)
        {
            // custom packages cannot be removed automatically
            return new ValueTask<PackageRemoveResult>(PackageRemoveResult.None);
        }

        private async Task<string> LoadThirdPartyNoticesAsync(LibraryId id, CancellationToken token)
        {
            string result = null;

            using (var stream = await Storage.OpenLibraryFileReadAsync(id, ThirdPartyNoticesFileName, token))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }

            return result.IsNullOrEmpty() ? null : result;
        }
    }
}
