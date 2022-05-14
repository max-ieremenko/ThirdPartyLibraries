using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class ThirdPartyNoticesLicenseContext
{
    // license full name
    public string FullName { get; set; }

    // license public urls
    public List<string> HRefs { get; } = new();

    // license content
    public List<string> FileNames { get; } = new();

    // list of packages referenced by this license
    public List<ThirdPartyNoticesPackageContext> Packages { get; } = new();
}