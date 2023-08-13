using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository.Template;

namespace ThirdPartyLibraries.Suite.Refresh.Internal;

internal interface IPackageReadMeUpdater
{
    Task<RootReadMePackageContext?> UpdateAsync(LibraryId id, CancellationToken token);
}