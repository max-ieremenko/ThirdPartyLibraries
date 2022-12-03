namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters;

internal sealed class NpmConfiguration
{
    public NpmIgnoreFilterConfiguration IgnorePackages { get; set; } = new NpmIgnoreFilterConfiguration();

    public bool DownloadPackageIntoRepository { get; set; }
}