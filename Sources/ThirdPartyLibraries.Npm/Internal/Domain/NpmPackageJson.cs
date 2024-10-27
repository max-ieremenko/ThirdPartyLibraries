using System.Text.Json;

namespace ThirdPartyLibraries.Npm.Internal.Domain;

internal sealed class NpmPackageJson
{
    public string? Name { get; set; }

    public string? Version { get; set; }

    public string? Description { get; set; }

    public JsonElement License { get; set; }

    public JsonElement[]? Licenses { get; set; }

    public string? HomePage { get; set; }

    public JsonElement? Author { get; set; }

    public JsonElement? Contributors { get; set; }

    public JsonElement Repository { get; set; }

    public JsonElement Dependencies { get; set; }

    public JsonElement DevDependencies { get; set; }
}