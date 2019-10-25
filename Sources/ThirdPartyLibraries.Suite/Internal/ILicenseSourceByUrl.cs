using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal interface ILicenseSourceByUrl
    {
        Task<LicenseInfo> DownloadByUrlAsync(string url, CancellationToken token);
    }
}
