using System.Text.Json;

namespace ThirdPartyLibraries.NuGet.Internal.Domain;

internal sealed class ProjectAssetsJson
{
    public int Version { get; set; }

    public JsonElement Targets { get; set; }

    public ProjectAssetsProjectJson Project { get; set; } = null!;
}