namespace ThirdPartyLibraries.Repository.Template;

public sealed class RootReadMeLicenseContext
{
    public string Code { get; set; }

    public bool RequiresApproval { get; set; }

    public bool RequiresThirdPartyNotices { get; set; }

    public string LocalHRef { get; set; }

    public int PackagesCount { get; set; }
}