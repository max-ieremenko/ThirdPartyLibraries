using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal sealed class NuGetLicenseSource : ILicenseSourceByUrl
    {
        public NuGetLicenseSource(INuGetApi nuGetApi)
        {
            NuGetApi = nuGetApi;
        }

        public INuGetApi NuGetApi { get; }

        public async Task<LicenseInfo> DownloadByUrlAsync(string url, CancellationToken token)
        {
            url.AssertNotNull(nameof(url));

            var code = await NuGetApi.ResolveLicenseCodeAsync(url, token).ConfigureAwait(false);
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
