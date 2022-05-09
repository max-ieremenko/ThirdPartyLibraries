namespace ThirdPartyLibraries.Repository.Template;

public sealed class ThirdPartyNoticesPackageContext
{
    // package name
    public string Name { get; set; }

    // package public url
    public string HRef { get; set; }

    // package author(s)
    public string Author { get; set; }

    // package copyright
    public string Copyright { get; set; }

    // optional notices, saved into repository
    public string ThirdPartyNotices { get; set; }

    // repository license assigned to this package
    public ThirdPartyNoticesLicenseContext License { get; set; }

    // license assigned to this package, based on the package specification
    public ThirdPartyNoticesPackageLicenseContext PackageLicense { get; set; }
}