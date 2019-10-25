using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal interface IPackageResolver
    {
        Task<Package> DownloadAsync(LibraryId id, CancellationToken token);

        Task TryUpdateLicenseAsync(Package package, CancellationToken token);
    }
}
