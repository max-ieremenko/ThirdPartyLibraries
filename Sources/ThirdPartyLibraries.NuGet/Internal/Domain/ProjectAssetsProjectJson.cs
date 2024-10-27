using System.Text.Json;

namespace ThirdPartyLibraries.NuGet.Internal.Domain;

internal sealed class ProjectAssetsProjectJson
{
    public ProjectAssetsProjectRestoreJson Restore { get; set; } = null!;

    public JsonElement Frameworks { get; set; }
}