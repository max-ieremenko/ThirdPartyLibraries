using System.Text.Json;
using NuGet.Frameworks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.NuGet.Internal.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet.Internal;

internal readonly struct ProjectAssetsParser
{
    public const string FileName = "project.assets.json";

    public ProjectAssetsParser(ProjectAssetsJson content)
    {
        if (content.Version != 3)
        {
            throw new NotSupportedException($"{FileName} version {content.Version} is not supported");
        }

        Content = content;
    }

    public ProjectAssetsJson Content { get; }

    public static ProjectAssetsParser FromFile(string fileName)
    {
        using (var stream = File.OpenRead(fileName))
        {
            return FromStream(stream);
        }
    }

    public static ProjectAssetsParser FromStream(Stream stream)
    {
        var content = stream.JsonDeserialize(DomainJsonSerializerContext.Default.ProjectAssetsJson);
        return new ProjectAssetsParser(content);
    }

    public string GetProjectName() => Content.Project.Restore.ProjectName;

    public string[] GetTargetFrameworks()
    {
        var frameworks = Content.Project.Frameworks;
        return frameworks.EnumerateObject().Select(i => i.Name).ToArray();
    }

    public IEnumerable<(LibraryId Package, List<LibraryId> Dependencies)> GetReferences(string targetFramework)
    {
        var framework = MapTargetFrameworkProjFormatToNuGetFormat(targetFramework);

        var projectFrameworks = Content.Project.Frameworks;
        if (!projectFrameworks.TryGetProperty(targetFramework, out var projectFramework))
        {
            var frameworks = projectFrameworks.EnumerateObject().Select(i => i.Name);
            var frameworksText = string.Join(", ", frameworks);
            throw new InvalidOperationException($"project/frameworks/{targetFramework} not found in {frameworksText}.");
        }

        if (!projectFramework.TryGetProperty("dependencies", out var projectDependencies))
        {
            return [];
        }

        var targetPackageByName = ParseTarget(framework);
        foreach (var row in projectDependencies.EnumerateObject())
        {
            var targetName = row.Value.GetProperty("target").GetString();
            if (!"package".Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!targetPackageByName.TryGetValue(row.Name, out var target))
            {
                throw new InvalidOperationException($"The project {GetProjectName()} contains invalid reference to a package {row.Name}.");
            }

            target.IsRoot = true;
        }

        var dependenciesByPackage = new Dictionary<LibraryId, List<LibraryId>>();
        foreach (var name in targetPackageByName.Values.Where(i => i.IsRoot).Select(i => i.Id.Name))
        {
            AddPackage(dependenciesByPackage, targetPackageByName, name);
        }

        return dependenciesByPackage.Select(i => (i.Key, i.Value));
    }

    public List<Uri> GetPackageSources()
    {
        var result = new List<Uri>();

        var sources = Content.Project.Restore.Sources;
        if (sources.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in sources.EnumerateObject())
            {
                if (Uri.TryCreate(property.Name, UriKind.Absolute, out var path))
                {
                    result.Add(path);
                }
            }
        }

        return result;
    }

    internal static string MapTargetFrameworkProjFormatToNuGetFormat(string projFormat)
    {
        var framework = NuGetFramework.Parse(projFormat);
        return framework.ToString();
    }

    private static void AddPackage(
        Dictionary<LibraryId, List<LibraryId>> dependenciesByPackage,
        IDictionary<string, TargetPackage> targetPackageByName,
        string packageName)
    {
        var package = targetPackageByName[packageName];
        if (package.Ignore || dependenciesByPackage.ContainsKey(package.Id))
        {
            return;
        }

        var dependencies = new List<LibraryId>();
        dependenciesByPackage.Add(package.Id, dependencies);

        foreach (var name in package.GetDependencies())
        {
            AddPackage(dependenciesByPackage, targetPackageByName, name);

            var dependency = targetPackageByName[name];
            if (!dependency.Ignore)
            {
                dependencies.Add(dependency.Id);
            }
        }
    }

    private IDictionary<string, TargetPackage> ParseTarget(string frameworkName)
    {
        var targets = Content.Targets;
        var target = targets.EnumerateObject().FirstOrDefault(i => i.Name.Equals(frameworkName, StringComparison.OrdinalIgnoreCase));
        if (target.Value.ValueKind != JsonValueKind.Object)
        {
            var frameworks = targets.EnumerateObject().Select(i => i.Name);
            var frameworksText = string.Join(", ", frameworks);
            throw new InvalidOperationException($"Target {frameworkName} not found in {frameworksText}.");
        }

        var result = new Dictionary<string, TargetPackage>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in target.Value.EnumerateObject())
        {
            var type = row.Value.GetProperty("type").GetString();
            if ("package".Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                var targetPackage = new TargetPackage(row.Name, row.Value);
                result.Add(targetPackage.Id.Name, targetPackage);
            }
        }

        return result;
    }

    private sealed class TargetPackage
    {
        private readonly JsonElement _content;
        private readonly bool _ignoreByRuntime;

        public TargetPackage(string fullName, JsonElement content)
        {
            _content = content;
            var index = fullName.IndexOf('/');
            Id = NuGetLibraryId.New(fullName.Substring(0, index), fullName.Substring(index + 1));

            _ignoreByRuntime = !content.TryGetProperty("runtime", out var runtime)
                               || runtime.EnumerateObject().All(i => i.Name.EndsWith("/_._", StringComparison.OrdinalIgnoreCase));
        }

        public LibraryId Id { get; }

        public bool IsRoot { get; set; }

        public bool Ignore => !IsRoot && _ignoreByRuntime;

        public IEnumerable<string> GetDependencies()
        {
            if (!_content.TryGetProperty("dependencies", out var dependenciesSource))
            {
                return [];
            }

            return dependenciesSource.EnumerateObject().Select(i => i.Name);
        }
    }
}