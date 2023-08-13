using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal interface ILicenseByUrlResolver
{
    Task<LicenseSpec?> TryResolveAsync(string url, CancellationToken token);
}