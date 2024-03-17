using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.GitHub.Internal;

internal sealed class GitHubRepositoryNameParser : IRepositoryNameParser
{
    public bool TryGetRepository(Uri url, [NotNullWhen(true)] out string? owner, [NotNullWhen(true)] out string? name)
    {
        return GitHubUrlParser.TryParseRepository(url, out owner, out name);
    }
}