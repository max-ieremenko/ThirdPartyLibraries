namespace ThirdPartyLibraries.Npm.Internal.Domain;

internal sealed class NpmPackageIndex
{
    public Dictionary<string, NpmPackageIndexVersion>? Versions { get; set; }
}