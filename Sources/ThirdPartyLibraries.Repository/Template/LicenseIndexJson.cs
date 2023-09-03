using System;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class LicenseIndexJson
{
    public string Code { get; set; } = null!;

    public string? FullName { get; set; }

    public bool RequiresApproval { get; set; }

    public bool RequiresThirdPartyNotices { get; set; }

    public string? HRef { get; set; }

    public string? FileName { get; set; }

    public string[] Dependencies { get; set; } = Array.Empty<string>();
}