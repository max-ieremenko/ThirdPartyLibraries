namespace ThirdPartyLibraries.Repository.Template;

public sealed class CustomLibraryIndexJson
{
    public string Name { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string? LicenseCode { get; set; }

    public string? HRef { get; set; }

    public string? Author { get; set; }

    public string? Copyright { get; set; }

    public IList<Application> UsedBy { get; } = new List<Application>();
}