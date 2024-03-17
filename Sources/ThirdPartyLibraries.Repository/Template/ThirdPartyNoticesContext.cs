namespace ThirdPartyLibraries.Repository.Template;

public sealed class ThirdPartyNoticesContext
{
    // Application Title, comes from command line
    public string Title { get; set; } = null!;

    // list of repository licenses referenced by packages
    public List<ThirdPartyNoticesLicenseContext> Licenses { get; } = new();

    // list of packages
    public List<ThirdPartyNoticesPackageContext> Packages { get; } = new();
}