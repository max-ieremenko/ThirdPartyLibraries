namespace ThirdPartyLibraries.Repository.Template;

[DebuggerDisplay("{Name}")]
public sealed class RootReadMePackageContext
{
    public string Source { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string? License { get; set; }

    public string? LicenseLocalHRef { get; set; }

    public string? LicenseMarkdownExpression { get; set; }

    public bool IsApproved { get; set; }

    public string LocalHRef { get; set; } = null!;

    public string? SourceHRef { get; set; }

    public string UsedBy { get; set; } = null!;
}