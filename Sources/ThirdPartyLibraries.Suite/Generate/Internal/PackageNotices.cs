using System;
using System.Diagnostics;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

[DebuggerDisplay("{Name} {Version}")]
internal sealed class PackageNotices
{
    public string Name { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string? LicenseCode { get; set; }

    public Uri HRef { get; set; } = null!;

    public Uri? LicenseHRef { get; set; }

    public PackageLicenseFile? LicenseFile { get; set; }

    public string? Author { get; set; }

    public string? Copyright { get; set; }

    public string? ThirdPartyNotices { get; set; }
}