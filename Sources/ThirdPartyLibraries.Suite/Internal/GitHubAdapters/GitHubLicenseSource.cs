using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.GitHub;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.GitHubAdapters
{
    internal sealed class GitHubLicenseSource : ILicenseSourceByUrl
    {
        [Dependency]
        public IGitHubApi GitHubApi { get; set; }

        [Dependency]
        public GitHubConfiguration Configuration { get; set; }

        public async Task<LicenseInfo> DownloadByUrlAsync(string url, CancellationToken token)
        {
            url.AssertNotNull(nameof(url));

            var license = await GitHubApi.LoadLicenseAsync(url, Configuration.PersonalAccessToken, token);
            if (license == null)
            {
                return null;
            }

            return CreateLicenseInfo(license.Value);
        }

        private static LicenseInfo CreateLicenseInfo(GitHubLicense license)
        {
            return new LicenseInfo
            {
                Code = license.SpdxId,
                CodeHRef = license.SpdxIdHRef,
                FileHRef = license.FileContentHRef,
                FileName = license.FileName,
                FileContent = license.FileContent,
            };
        }
    }
}
