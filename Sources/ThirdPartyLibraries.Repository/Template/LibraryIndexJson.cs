namespace ThirdPartyLibraries.Repository.Template;

public sealed class LibraryIndexJson
{
    public string? Source { get; set; }

    public LicenseConclusion License { get; } = new();

    public List<Application> UsedBy { get; } = new();

    public List<LibraryLicense> Licenses { get; } = new();
}