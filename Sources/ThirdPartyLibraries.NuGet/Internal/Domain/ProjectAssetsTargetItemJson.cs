using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.NuGet.Internal.Domain;

internal sealed class ProjectAssetsTargetItemJson
{
    [JsonIgnore]
    public string Name { get; set; } = null!;

    public string? Type { get; set; }
}