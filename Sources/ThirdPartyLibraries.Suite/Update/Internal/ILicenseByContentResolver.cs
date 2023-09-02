using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal interface ILicenseByContentResolver
{
    Task<IdenticalLicenseFile?> TryResolveAsync(LibraryId library, CancellationToken token);
}