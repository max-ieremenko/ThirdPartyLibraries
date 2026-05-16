using System.Text.Json;

namespace ThirdPartyLibraries.NuGet.Internal.Domain;

internal sealed class ProjectAssetsProjectJson
{
    public ProjectAssetsProjectRestoreJson Restore { get; set; } = null!;

    public JsonElement Frameworks { get; set; }

    internal ProjectAssetsProjectFrameworkJson FindFramework(string name)
    {
        if (!Frameworks.TryGetProperty(name, out var framework))
        {
            var names = Frameworks.EnumerateObject().Select(i => i.Name);
            var namesText = string.Join(", ", names);
            throw new InvalidOperationException($"project/frameworks/{name} not found in [{namesText}].");
        }

        return framework.Deserialize<ProjectAssetsProjectFrameworkJson>(DomainJsonSerializerContext.Default.ProjectAssetsProjectFrameworkJson)!;
    }
}