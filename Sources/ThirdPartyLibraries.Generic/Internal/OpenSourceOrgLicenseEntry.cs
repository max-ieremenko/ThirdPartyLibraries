using System;
using System.Collections.Generic;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic.Internal;

internal sealed class OpenSourceOrgLicenseEntry
{
    public OpenSourceOrgLicenseEntry(string code, string? fullName)
    {
        Code = code;
        FullName = fullName;
        Urls = new HashSet<Uri>(0, UriSimpleComparer.Instance);
    }

    public string Code { get; set; }

    public string? FullName { get; }

    public HashSet<Uri> Urls { get; }

    public Uri? DownloadUrl { get; set; }
}