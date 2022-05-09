using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class ThirdPartyNoticesContext
{
    // Application Title, comes from command line
    public string Title { get; set; }

    // list of repository licenses referenced by packages
    public IList<ThirdPartyNoticesLicenseContext> Licenses { get; } = new List<ThirdPartyNoticesLicenseContext>();

    // list of packages
    public IList<ThirdPartyNoticesPackageContext> Packages { get; } = new List<ThirdPartyNoticesPackageContext>();
}