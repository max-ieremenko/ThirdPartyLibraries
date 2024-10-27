using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.Generic.Internal.Domain;

internal sealed class OpenSourceOrgLicenseText
{
    public string? Url { get; set; }

    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }
}