using System.Text.Json;

namespace ThirdPartyLibraries.NuGet.Internal.Domain;

internal sealed class ProjectAssetsJson
{
    public int Version { get; set; }

    public JsonElement Targets { get; set; }

    public ProjectAssetsProjectJson Project { get; set; } = null!;

    internal JsonElement.ObjectEnumerator EnumerateTargetItems(string targetName, string? targetAlternativeName)
    {
        var target = Targets
            .EnumerateObject()
            .FirstOrDefault(i => i.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase) || i.Name.Equals(targetAlternativeName, StringComparison.OrdinalIgnoreCase));
        if (target.Value.ValueKind != JsonValueKind.Object)
        {
            var names = Targets.EnumerateObject().Select(i => i.Name);
            var namesText = string.Join(", ", names);
            throw new InvalidOperationException($"targets/{targetName} and targets/{targetAlternativeName} not found in [{namesText}].");
        }

        return target.Value.EnumerateObject();
    }
}