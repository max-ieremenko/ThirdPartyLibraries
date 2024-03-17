using Newtonsoft.Json;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class LibraryIndexJson
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Source { get; set; }

    public LicenseConclusion License { get; } = new();

    public List<Application> UsedBy { get; } = new();

    public List<LibraryLicense> Licenses { get; } = new();
}