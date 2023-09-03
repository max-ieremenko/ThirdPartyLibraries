using System.Collections.Generic;
using System.Diagnostics;

namespace ThirdPartyLibraries.Repository.Template;

[DebuggerDisplay("{FullName}")]
public sealed class ThirdPartyNoticesLicenseContext
{
    // license full name
    public string FullName { get; set; } = null!;

    // license public urls
    public List<string> HRefs { get; } = new();

    // license content
    public List<string> FileNames { get; } = new();

    // list of packages referenced by this license
    public List<ThirdPartyNoticesPackageContext> Packages { get; } = new();
}