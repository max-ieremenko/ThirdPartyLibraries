using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class ThirdPartyNoticesLicenseContext
{
    // license full name
    public string FullName { get; set; }

    // license public urls
    public IList<string> HRefs { get; } = new List<string>();

    // license content
    public IList<string> FileNames { get; } = new List<string>();

    // list of packages referenced by this license
    public IList<ThirdPartyNoticesPackageContext> Packages { get; } = new List<ThirdPartyNoticesPackageContext>();
}