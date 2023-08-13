using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet.Internal;

internal readonly struct ProjectAssetsParser
{
    public const string FileName = "project.assets.json";

    public ProjectAssetsParser(JObject content)
    {
        var version = content.Value<string>("version");
        if (version != "3")
        {
            throw new NotSupportedException($"{FileName} version {version} is not supported");
        }

        Content = content;
    }

    public JObject Content { get; }

    public static ProjectAssetsParser FromFile(string fileName)
    {
        using (var stream = File.OpenRead(fileName))
        {
            return FromStream(stream);
        }
    }

    public static ProjectAssetsParser FromStream(Stream stream)
    {
        var content = stream.JsonDeserialize<JObject>();
        return new ProjectAssetsParser(content);
    }

    public string GetProjectName() => Content.Value<JObject>("project")!.Value<JObject>("restore")!.Value<string>("projectName")!;

    public string[] GetTargetFrameworks()
    {
        var frameworks = Content.Value<JObject>("project")!.Value<JObject>("frameworks")!;

        return frameworks.Properties().Select(i => i.Name).ToArray();
    }

    public IEnumerable<(LibraryId Package, List<LibraryId> Dependencies)> GetReferences(string targetFramework)
    {
        var framework = MapTargetFrameworkProjFormatToNuGetFormat(targetFramework);

        var projectFrameworks = Content.Value<JObject>("project")!.Value<JObject>("frameworks")!;
        var projectFramework = projectFrameworks.Value<JObject>(targetFramework);
        if (projectFramework == null)
        {
            var frameworks = projectFrameworks.Properties().Select(i => i.Name);
            var frameworksText = string.Join(", ", frameworks);
            throw new InvalidOperationException($"project/frameworks/{targetFramework} not found in {frameworksText}.");
        }

        var projectDependencies = projectFramework.Value<JObject>("dependencies");
        if (projectDependencies == null)
        {
            return Enumerable.Empty<(LibraryId, List<LibraryId>)>();
        }

        var targetPackageByName = ParseTarget(framework);
        foreach (var row in projectDependencies)
        {
            var targetName = row.Value!.Value<string>("target");
            if (!"package".Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!targetPackageByName.TryGetValue(row.Key, out var target))
            {
                throw new InvalidOperationException($"The project {GetProjectName()} contains invalid reference to a package {row.Key}.");
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
        var targets = Content.Value<JObject>("targets");
        var target = (JObject)targets!.GetValue(frameworkName, StringComparison.OrdinalIgnoreCase)!;
        if (target == null)
        {
            var frameworks = targets.Properties().Select(i => i.Name);
            var frameworksText = string.Join(", ", frameworks);
            throw new InvalidOperationException($"Target {frameworkName} not found in {frameworksText}.");
        }

        var result = new Dictionary<string, TargetPackage>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in target)
        {
            var type = row.Value!.Value<string>("type");
            if ("package".Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                var targetPackage = new TargetPackage(row.Key, (JObject)row.Value);
                result.Add(targetPackage.Id.Name, targetPackage);
            }
        }

        return result;
    }

    private sealed class TargetPackage
    {
        private readonly JObject _content;
        private readonly bool _ignoreByRuntime;

        public TargetPackage(string fullName, JObject content)
        {
            _content = content;
            var index = fullName.IndexOf('/');
            Id = NuGetLibraryId.New(fullName.Substring(0, index), fullName.Substring(index + 1));

            var runtime = content.Value<JObject>("runtime");
            _ignoreByRuntime = runtime == null || runtime.Properties().All(i => i.Name.EndsWith("/_._", StringComparison.OrdinalIgnoreCase));
        }

        public LibraryId Id { get; }

        public bool IsRoot { get; set; }

        public bool Ignore => !IsRoot && _ignoreByRuntime;

        public IEnumerable<string> GetDependencies()
        {
            var dependenciesSource = _content.Value<JObject>("dependencies");
            if (dependenciesSource == null)
            {
                return Enumerable.Empty<string>();
            }

            return dependenciesSource.Properties().Select(i => i.Name);
        }
    }
}