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

        Task UpdatePackageReadMeAsync(Package package, CancellationToken token);

        ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token);
    }
}
