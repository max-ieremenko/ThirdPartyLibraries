using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Suite.Internal;

internal interface IPackageResolver
{
    ValueTask<bool> DownloadAsync(LibraryId id, CancellationToken token);
}