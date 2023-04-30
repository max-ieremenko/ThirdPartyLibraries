using System;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters;

internal sealed class DefaultNuGetPackageUrlResolver : INuGetPackageUrlResolver
{
    public (string Text, string HRef) GetUserUrl(string packageName, string packageVersion, string source, string repositoryUrl)
    {
        var href = "https://" + KnownHosts.NuGetOrg + "/packages/" + Uri.EscapeDataString(packageName) + "/" + Uri.EscapeDataString(packageVersion);
        return (PackageSources.NuGet, href);
    }
}