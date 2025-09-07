using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class LibraryIndexJson
{
    public const string DefaultSchema = "https://raw.githubusercontent.com/max-ieremenko/ThirdPartyLibraries/refs/heads/master/Docs/schema.package-index.json";

    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    public string? Source { get; set; }

    public LicenseConclusion License { get; } = new();

    public List<Application> UsedBy { get; } = new();

    public List<LibraryLicense> Licenses { get; } = new();
}