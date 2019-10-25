using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal sealed class NuGetSourceCodeReferenceProvider : ISourceCodeReferenceProvider
    {
        [Dependency]
        public NuGetConfiguration Configuration { get; set; }

        public IEnumerable<LibraryReference> GetReferencesFrom(string path)
        {
            var result = Enumerable.Empty<LibraryReference>();

            if (File.Exists(path)
                && ProjectAssetsParser.FileName.EqualsIgnoreCase(Path.GetFileName(path)))
            {
                result = GetReferencesFromFile(path);
            }

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, ProjectAssetsParser.FileName, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    result = result.Concat(GetReferencesFromFile(file));
                }
            }

            return result;
        }

        private IEnumerable<LibraryReference> GetReferencesFromFile(string fileName)
        {
            var parser = ProjectAssetsParser.FromFile(fileName);

            var targetFrameworks = parser.GetTargetFrameworks();

            var internalFilter = new NuGetIgnoreFilter(Configuration.InternalPackages);
            var isInternalByProject = internalFilter.FilterByProjectName(parser.GetProjectName());

            foreach (var entry in GetFilteredReferences(parser, targetFrameworks))
            {
                var isInternal = isInternalByProject || internalFilter.FilterByName(entry.Package.Name);
                yield return new LibraryReference(
                    new LibraryId(PackageSources.NuGet, entry.Package.Name, entry.Package.Version), 
                    targetFrameworks,
                    entry.Dependencies,
                    isInternal);
            }
        }

        private IEnumerable<(NuGetPackageId Package, IList<LibraryId> Dependencies)> GetFilteredReferences(ProjectAssetsParser parser, string[] targetFrameworks)
        {
            var ignoreFilter = new NuGetIgnoreFilter(Configuration.IgnorePackages);
            if (ignoreFilter.FilterByProjectName(parser.GetProjectName()))
            {
               yield break;
            }

            foreach (var targetFramework in targetFrameworks)
            {
                foreach (var entry in parser.GetReferences(targetFramework))
                {
                    if (ignoreFilter.FilterByName(entry.Package.Name))
                    {
                        continue;
                    }

                    var dependencies = new List<LibraryId>(entry.Dependencies.Count);
                    foreach (var d in entry.Dependencies)
                    {
                        if (!ignoreFilter.FilterByName(d.Name))
                        {
                            dependencies.Add(new LibraryId(PackageSources.NuGet, d.Name, d.Version));
                        }
                    }

                    yield return (entry.Package, dependencies);
                }
            }
        }
    }
}
