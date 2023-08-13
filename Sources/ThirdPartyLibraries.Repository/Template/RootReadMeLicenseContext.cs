namespace ThirdPartyLibraries.Repository.Template;

public sealed class RootReadMeLicenseContext
{
    public string Code { get; set; } = null!;

    public bool RequiresApproval { get; set; }

    public bool RequiresThirdPartyNotices { get; set; }

    public string LocalHRef { get; set; } = null!;

    public int PackagesCount { get; set; }
}