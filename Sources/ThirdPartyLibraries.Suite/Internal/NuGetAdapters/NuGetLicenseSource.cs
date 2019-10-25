using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal sealed class NuGetLicenseSource : ILicenseSourceByUrl
    {
        [Dependency]
        public INuGetApi NuGetApi { get; set; }

        public async Task<LicenseInfo> DownloadByUrlAsync(string url, CancellationToken token)
        {
            url.AssertNotNull(nameof(url));

            var code = await NuGetApi.ResolveLicenseCodeAsync(url, token);
            if (code == null)
            {
                return null;
            }

            return new LicenseInfo
            {
                Code = code,
                CodeHRef = url
            };
        }
    }
}
