﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet
{
    public readonly struct ProjectAssetsParser
    {
        public const string FileName = "project.assets.json";

        public ProjectAssetsParser(JObject content)
        {
            content.AssertNotNull(nameof(content));

            var version = content.Value<string>("version");
            if (version != "3")
            {
                throw new NotSupportedException("{0} version {1} is not supported".FormatWith(FileName, version));
            }

            Content = content;
        }

        public JObject Content { get; }

        public static ProjectAssetsParser FromFile(string fileName)
        {
            fileName.AssertNotNull(nameof(fileName));

            using (var stream = File.OpenRead(fileName))
            {
                return FromStream(stream);
            }
        }

        public static ProjectAssetsParser FromStream(Stream stream)
        {
            stream.AssertNotNull(nameof(stream));

            JObject content;

            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                content = (JObject)new JsonSerializer().Deserialize(jsonReader);
            }

            return new ProjectAssetsParser(content);
        }

        public string GetProjectName() => Content.Value<JObject>("project").Value<JObject>("restore").Value<string>("projectName");

        public string[] GetTargetFrameworks()
        {
            var frameworks = Content.Value<JObject>("project").Value<JObject>("frameworks");

            return frameworks.Properties().Select(i => i.Name).ToArray();
        }

        public IEnumerable<(NuGetPackageId Package, IList<NuGetPackageId> Dependencies)> GetReferences(string targetFramework)
        {
            var framework = MapTargetFrameworkProjFormatToNuGetFormat(targetFramework);
            
            var projectFrameworks = Content.Value<JObject>("project").Value<JObject>("frameworks");
            var projectFramework = projectFrameworks.Value<JObject>(targetFramework);
            if (projectFramework == null)
            {
                var frameworks = projectFrameworks.Properties().Select(i => i.Name);
                throw new InvalidOperationException("project/frameworks/{0} not found in {1}.".FormatWith(targetFramework, string.Join(", ", frameworks)));
            }

            var projectDependencies = projectFramework.Value<JObject>("dependencies");
            if (projectDependencies == null)
            {
                return Enumerable.Empty<(NuGetPackageId, IList<NuGetPackageId>)>();
            }

            var targetPackageByName = ParseTarget(framework);
            foreach (var row in projectDependencies)
            {
                var targetName = row.Value.Value<string>("target");
                if ("package".EqualsIgnoreCase(targetName))
                {
                    targetPackageByName[row.Key].IsRoot = true;
                }
            }

            var dependenciesByPackage = new Dictionary<NuGetPackageId, IList<NuGetPackageId>>();
            foreach (var name in targetPackageByName.Values.Where(i => i.IsRoot).Select(i => i.Id.Name))
            {
                AddPackage(dependenciesByPackage, targetPackageByName, name);
            }
            
            return dependenciesByPackage.Select(i => (i.Key, i.Value));
        }

        private static string MapTargetFrameworkProjFormatToNuGetFormat(string projFormat)
        {
            const string core = "netcoreapp";
            const string standard = "netstandard";

            // netcoreapp2.2 => .NETCoreApp,Version=v2.2
            if (projFormat.StartsWithIgnoreCase(core))
            {
                var version = projFormat.Substring(core.Length);
                return ".NETCoreApp,Version=v" + version;
            }

            // netstandard2.0 => .NETStandard,Version=v2.0
            if (projFormat.StartsWithIgnoreCase(standard))
            {
                var version = projFormat.Substring(standard.Length);
                return ".NETStandard,Version=v" + version;
            }

            if ("net48".EqualsIgnoreCase(projFormat))
            {
                return ".NETFramework,Version=v4.8";
            }

            if ("net472".EqualsIgnoreCase(projFormat))
            {
                return ".NETFramework,Version=v4.7.2";
            }

            if ("net471".EqualsIgnoreCase(projFormat))
            {
                return ".NETFramework,Version=v4.7.1";
            }

            if ("net47".EqualsIgnoreCase(projFormat))
            {
                return ".NETFramework,Version=v4.7";
            }

            if ("net462".EqualsIgnoreCase(projFormat))
            {
                return ".NETFramework,Version=v4.6.2";
            }

            if ("net461".EqualsIgnoreCase(projFormat))
            {
                return ".NETFramework,Version=v4.6.1";
            }

            if ("net46".EqualsIgnoreCase(projFormat))
            {
                return ".NETFramework,Version=v4.6";
            }

            if ("net452".EqualsIgnoreCase(projFormat))
            {
                return ".NETFramework,Version=v4.5.2";
            }

            if ("net451".EqualsIgnoreCase(projFormat))
            {
                return ".NETFramework,Version=v4.5.1";
            }

            return projFormat;
        }

        private static void AddPackage(
            IDictionary<NuGetPackageId, IList<NuGetPackageId>> dependenciesByPackage,
            IDictionary<string, TargetPackage> targetPackageByName,
            string packageName)
        {
            var package = targetPackageByName[packageName];
            if (package.Ignore || dependenciesByPackage.ContainsKey(package.Id))
            {
                return;
            }

            var dependencies = new List<NuGetPackageId>();
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
            var target = (JObject)targets.GetValue(frameworkName, StringComparison.OrdinalIgnoreCase);
            if (target == null)
            {
                var frameworks = targets.Properties().Select(i => i.Name);
                throw new InvalidOperationException("Target {0} not found in {1}.".FormatWith(frameworkName, string.Join(", ", frameworks)));
            }

            var result = new Dictionary<string, TargetPackage>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in target)
            {
                var type = row.Value.Value<string>("type");
                if ("package".EqualsIgnoreCase(type))
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
                Id = new NuGetPackageId(fullName.Substring(0, index), fullName.Substring(index + 1));

                var runtime = content.Value<JObject>("runtime");
                _ignoreByRuntime = runtime == null || runtime.Properties().All(i => i.Name.EndsWithIgnoreCase("/_._"));
            }

            public NuGetPackageId Id { get; }

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
}
