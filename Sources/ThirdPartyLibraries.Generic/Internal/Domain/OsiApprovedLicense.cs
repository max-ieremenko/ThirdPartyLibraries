using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.Generic.Internal.Domain;

internal sealed class OsiApprovedLicense
{
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("spdx_id")]
    public string? SpdxId { get; set; }

    [JsonPropertyName("license_steward_url")]
    public string? LicenseStewardUrl { get; set; }
}