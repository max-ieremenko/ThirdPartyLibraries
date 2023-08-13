using System.Diagnostics;

namespace ThirdPartyLibraries.Repository.Template;

[DebuggerDisplay("{Name} {Version}")]
public sealed class ThirdPartyNoticesPackageContext
{
    // package name
    public string Name { get; set; } = null!;

    // package version
    public string Version { get; set; } = null!;

    // package public url
    public string HRef { get; set; } = null!;

    // package author(s)
    public string? Author { get; set; }

    // package copyright
    public string? Copyright { get; set; }

    // optional notices, saved into repository
    public string? ThirdPartyNotices { get; set; }

    // repository license assigned to this package
    public ThirdPartyNoticesLicenseContext License { get; set; } = null!;

    // license assigned to this package, based on the package specification
    public ThirdPartyNoticesPackageLicenseContext PackageLicense { get; set; } = null!;
}