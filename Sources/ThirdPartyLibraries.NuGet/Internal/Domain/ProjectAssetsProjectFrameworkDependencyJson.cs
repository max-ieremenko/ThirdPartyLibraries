using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.NuGet.Internal.Domain;

internal sealed class ProjectAssetsProjectFrameworkDependencyJson
{
    [JsonIgnore]
    public string Name { get; set; } = null!;

    public string? Target { get; set; }
}