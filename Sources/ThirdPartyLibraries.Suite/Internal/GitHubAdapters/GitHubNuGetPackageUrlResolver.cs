using System;
using ThirdPartyLibraries.GitHub;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.GitHubAdapters;

internal sealed class GitHubNuGetPackageUrlResolver : INuGetPackageUrlResolver
{
    private readonly IGitHubApi _gitHubApi;

    public GitHubNuGetPackageUrlResolver(IGitHubApi gitHubApi)
    {
        _gitHubApi = gitHubApi;
    }

    public (string Text, string HRef) GetUserUrl(string packageName, string packageVersion, string source, string repositoryUrl)
    {
        if (!TryExtractRepositoryName(repositoryUrl, out var owner, out var repository))
        {
            owner = GetOwner(new Uri(source, UriKind.Absolute).AbsolutePath);
            repository = GetRepository(packageName);
        }

        var href = "https://" + KnownHosts.GitHub + "/" + owner + "/" + repository + "/pkgs/nuget/" + Uri.EscapeDataString(packageName);
        return (KnownHosts.GitHub, href);
    }

    private static string GetOwner(ReadOnlySpan<char> sourcePath)
    {
        if (sourcePath[0] == '/')
        {
            sourcePath = sourcePath.Slice(1);
        }

        var index = sourcePath.IndexOf('/');
        return sourcePath.Slice(0, index).ToString();
    }

    private static string GetRepository(string packageName)
    {
        var index = packageName.IndexOf('.');
        if (index > 0)
        {
            return packageName.Substring(0, index);
        }

        return packageName;
    }

    private bool TryExtractRepositoryName(string url, out string owner, out string name)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            owner = null;
            name = null;
            return false;
        }

        return _gitHubApi.TryExtractRepositoryName(url, out owner, out name);
    }
}