using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.GitHub;

public interface IGitHubApi
{
    Task<GitHubLicense?> LoadLicenseAsync(string licenseUrl, string authorizationToken, CancellationToken token);

    bool TryExtractRepositoryName(string url, out string owner, out string name);
}