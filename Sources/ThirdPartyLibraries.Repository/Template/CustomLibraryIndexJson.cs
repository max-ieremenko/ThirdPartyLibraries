using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class CustomLibraryIndexJson
{
    public const string DefaultSchema = "https://raw.githubusercontent.com/max-ieremenko/ThirdPartyLibraries/refs/heads/master/Docs/schema.custom-package-index.json";

    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    public string Name { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string? LicenseCode { get; set; }

    public string? HRef { get; set; }

    public string? Author { get; set; }

    public string? Copyright { get; set; }

    public IList<Application> UsedBy { get; } = new List<Application>();
}