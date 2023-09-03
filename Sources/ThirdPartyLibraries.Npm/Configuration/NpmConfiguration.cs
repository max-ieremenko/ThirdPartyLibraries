namespace ThirdPartyLibraries.Npm.Configuration;

public sealed class NpmConfiguration
{
    public const string SectionName = "npmjs.com";

    public NpmIgnoreFilterConfiguration IgnorePackages { get; set; } = new();

    public bool DownloadPackageIntoRepository { get; set; }
}