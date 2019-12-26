using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPartyLibraries.Npm;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters
{
    internal sealed class NpmSourceCodeReferenceProvider : ISourceCodeReferenceProvider
    {
        [Dependency]
        public NpmConfiguration Configuration { get; set; }

        public IEnumerable<LibraryReference> GetReferencesFrom(string path)
        {
            var result = Enumerable.Empty<LibraryReference>();

            if (File.Exists(path)
                && PackageJsonParser.FileName.EqualsIgnoreCase(Path.GetFileName(path)))
            {
                result = GetReferencesFromFile(path);
            }

            if (Directory.Exists(path))
            {
                var files = FindAllPackageJson(path);
                foreach (var file in files)
                {
                    result = result.Concat(GetReferencesFromFile(file));
                }
            }

            return result.Where(i => i != null);
        }

        private static IEnumerable<string> FindAllPackageJson(string path)
        {
            var fileName = Path.Combine(path, PackageJsonParser.FileName);
            if (File.Exists(fileName))
            {
                // project root
                return new[] { fileName };
            }

            return Directory.GetDirectories(path).SelectMany(FindAllPackageJson);
        }

        private static LibraryReference ReadFromNodeModules(
            NpmPackageId dependency,
            string nodeModulesDirectoryName,
            bool isInternal)
        {
            var fileName = Path.Combine(nodeModulesDirectoryName, dependency.Name, PackageJsonParser.FileName);
            if (!File.Exists(fileName))
            {
                // TODO: npm root -g
                // throw new FileNotFoundException("File {0} not found.".FormatWith(fileName));
                return null;
            }

            var parser = PackageJsonParser.FromFile(fileName);

            return new LibraryReference(
                new LibraryId(PackageSources.Npm, parser.GetName(), parser.GetVersion()),
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                isInternal);
        }

        private IEnumerable<LibraryReference> GetReferencesFromFile(string fileName)
        {
            var folderName = Path.GetFileName(Path.GetDirectoryName(fileName));
            if (new IgnoreFilter(Configuration.IgnorePackages.ByFolderName).Filter(folderName))
            {
                yield break;
            }

            var nodeModulesDirectoryName = Path.Combine(Path.GetDirectoryName(fileName), PackageJsonParser.NodeModules);
            if (!Directory.Exists(nodeModulesDirectoryName))
            {
                throw new DirectoryNotFoundException("Directory {0} not found.".FormatWith(nodeModulesDirectoryName));
            }

            var parser = PackageJsonParser.FromFile(fileName);
            var ignoreByName = new IgnoreFilter(Configuration.IgnorePackages.ByName);

            foreach (var dependency in parser.GetDependencies())
            {
                if (!ignoreByName.Filter(dependency.Name))
                {
                    yield return ReadFromNodeModules(dependency, nodeModulesDirectoryName, false);
                }
            }

            foreach (var dependency in parser.GetDevDependencies())
            {
                if (!ignoreByName.Filter(dependency.Name))
                {
                    yield return ReadFromNodeModules(dependency, nodeModulesDirectoryName, true);
                }
            }
        }
    }
}
