using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal interface IPackageLicenseUpdater
{
    Task<bool> UpdateAsync(LibraryId library, CancellationToken token);
}