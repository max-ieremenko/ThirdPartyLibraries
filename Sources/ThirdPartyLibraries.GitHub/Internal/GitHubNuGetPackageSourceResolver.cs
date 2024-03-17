using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.GitHub.Internal;

internal sealed class GitHubNuGetPackageSourceResolver : INuGetPackageSourceResolver
{
    public bool TryResolve(IPackageSpec spec, Uri packageSource, [NotNullWhen(true)] out PackageSource? source)
    {
        if (!GitHubUrlParser.TryParseNuGetOwner(packageSource, out var packageSourceOwner))
        {
            source = null;
            return false;
        }

        string? owner = null;
        string? repository = null;
        if (TryParseRepository(spec.GetRepositoryUrl(), out var repositoryOwner, out var repositoryName)
            && repositoryOwner.Equals(packageSourceOwner, StringComparison.OrdinalIgnoreCase))
        {
            owner = repositoryOwner;
            repository = repositoryName;
        }

        if (owner == null)
        {
            owner = packageSourceOwner;
        }

        if (repository == null)
        {
            repository = GetRepositoryFromPackageName(spec.GetName());
        }

        var downloadUrl = "https://" + GitHubUrlParser.Host + "/" + owner + "/" + repository + "/pkgs/nuget/" + Uri.EscapeDataString(spec.GetName());
        source = new PackageSource(GitHubUrlParser.Host, new Uri(downloadUrl));
        return true;
    }

    private static bool TryParseRepository(
        string? repositoryUrl,
        [NotNullWhen(true)] out string? owner,
        [NotNullWhen(true)] out string? repository)
    {
        if (string.IsNullOrEmpty(repositoryUrl) || !Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var url))
        {
            owner = null;
            repository = null;
            return false;
        }

        return GitHubUrlParser.TryParseRepository(url, out owner, out repository);
    }

    private static string GetRepositoryFromPackageName(string packageName)
    {
        var index = packageName.IndexOf('.');
        if (index > 0)
        {
            return packageName.Substring(0, index);
        }

        return packageName;
    }
}