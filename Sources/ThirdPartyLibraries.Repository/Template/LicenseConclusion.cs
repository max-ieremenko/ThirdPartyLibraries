namespace ThirdPartyLibraries.Repository.Template;

public sealed class LicenseConclusion
{
    public LicenseConclusion()
    {
    }

    public LicenseConclusion(string? code, string? status)
    {
        Code = code;
        Status = status;
    }

    public string? Code { get; set; }

    public string? Status { get; set; }
}