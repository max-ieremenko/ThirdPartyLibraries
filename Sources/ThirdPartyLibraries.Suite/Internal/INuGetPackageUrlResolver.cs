namespace ThirdPartyLibraries.Suite.Internal;

internal interface INuGetPackageUrlResolver
{
    (string Text, string HRef) GetUserUrl(string packageName, string packageVersion, string source, string repositoryUrl);
}