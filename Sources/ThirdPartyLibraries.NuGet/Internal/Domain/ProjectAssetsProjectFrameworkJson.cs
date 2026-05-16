using System.Text.Json;

namespace ThirdPartyLibraries.NuGet.Internal.Domain;

internal sealed class ProjectAssetsProjectFrameworkJson
{
    public string? TargetAlias { get; set; }

    public JsonElement Dependencies { get; set; }

    internal IEnumerable<ProjectAssetsProjectFrameworkDependencyJson> EnumerateDependencies()
    {
        foreach (var row in Dependencies.EnumerateObject())
        {
            var dependency = row.Value.Deserialize<ProjectAssetsProjectFrameworkDependencyJson>(DomainJsonSerializerContext.Default.ProjectAssetsProjectFrameworkDependencyJson);
            dependency!.Name = row.Name;
            yield return dependency;
        }
    }
}