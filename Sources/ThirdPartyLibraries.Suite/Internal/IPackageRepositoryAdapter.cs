using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal interface IPackageRepositoryAdapter
    {
        IStorage Storage { get; }

        Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token);

        Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token);

        Task<PackageReadMe> UpdatePackageReadMeAsync(LibraryId id, CancellationToken token);

        Task<PackageNotices> LoadPackageNoticesAsync(LibraryId id, CancellationToken token);

        ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token);
    }
}
