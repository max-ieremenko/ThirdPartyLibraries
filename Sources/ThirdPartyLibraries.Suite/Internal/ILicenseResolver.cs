using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal interface ILicenseResolver
    {
        Task<LicenseInfo> ResolveByUrlAsync(string url, CancellationToken token);

        Task<LicenseInfo> DownloadByCodeAsync(string code, CancellationToken token);
    }
}
