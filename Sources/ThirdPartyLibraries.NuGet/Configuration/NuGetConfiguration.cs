namespace ThirdPartyLibraries.NuGet.Configuration;

public sealed class NuGetConfiguration
{
    public const string SectionName = "nuget.org";

    public NuGetIgnoreFilterConfiguration IgnorePackages { get; set; } = new();

    public NuGetIgnoreFilterConfiguration InternalPackages { get; set; } = new();

    public bool AllowToUseLocalCache { get; set; }

    public bool DownloadPackageIntoRepository { get; set; }
}