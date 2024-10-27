using System.Text.Json;

namespace ThirdPartyLibraries.NuGet.Internal.Domain;

internal sealed class ProjectAssetsProjectRestoreJson
{
    public string ProjectName { get; set; } = null!;

    public JsonElement Sources { get; set; }
}