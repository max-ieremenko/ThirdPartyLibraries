using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal interface ILicenseByCodeResolver
{
    Task<LicenseSpec?> TryResolveAsync(string code, CancellationToken token);
}