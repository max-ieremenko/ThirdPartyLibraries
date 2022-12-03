using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPartyLibraries.Npm;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters;

internal sealed class NpmSourceCodeReferenceProvider : ISourceCodeReferenceProvider
{
    private string _npmRoot;

    public NpmSourceCodeReferenceProvider(INpmApi api, NpmConfiguration configuration)
    {
        Api = api;
        Configuration = configuration;
    }

    public INpmApi Api { get; }

    public NpmConfiguration Configuration { get; }

    public void AddReferencesFrom(string path, IList<LibraryReference> references, ICollection<LibraryId> notFound)
    {
        if (File.Exists(path)
            && PackageJsonParser.FileName.EqualsIgnoreCase(Path.GetFileName(path)))
        {
            AddReferencesFromFile(path, references, notFound);
        }

        if (Directory.Exists(path))
        {
            var files = FindAllPackageJson(path);
            foreach (var file in files)
            {
                AddReferencesFromFile(file, references, notFound);
            }
        }
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

    private LibraryReference ReadFromNodeModules(
        NpmPackageId dependency,
        string nodeModulesDirectoryName,
        bool isInternal)
    {
        var fileName = Path.Combine(nodeModulesDirectoryName, dependency.Name, PackageJsonParser.FileName);
        if (!File.Exists(fileName))
        {
            fileName = Path.Combine(GetNpmRoot(), dependency.Name, PackageJsonParser.FileName);
            if (!File.Exists(fileName))
            {
                // throw new FileNotFoundException("File {0} not found.".FormatWith(fileName));
                return null;
            }
        }

        var parser = PackageJsonParser.FromFile(fileName);

        return new LibraryReference(
            new LibraryId(PackageSources.Npm, parser.GetName(), parser.GetVersion()),
            Array.Empty<string>(),
            Array.Empty<LibraryId>(),
            isInternal);
    }

    private void AddReferencesFromFile(
        string fileName,
        IList<LibraryReference> references,
        ICollection<LibraryId> notFound)
    {
        var folderName = Path.GetFileName(Path.GetDirectoryName(fileName));
        if (new IgnoreFilter(Configuration.IgnorePackages.ByFolderName).Filter(folderName))
        {
            return;
        }

        var nodeModulesDirectoryName = Path.Combine(Path.GetDirectoryName(fileName), PackageJsonParser.NodeModules);
        if (!Directory.Exists(nodeModulesDirectoryName))
        {
            throw new DirectoryNotFoundException("Directory {0} not found. Did you run \"npm restore\"?".FormatWith(nodeModulesDirectoryName));
        }

        var parser = PackageJsonParser.FromFile(fileName);
        var ignoreByName = new IgnoreFilter(Configuration.IgnorePackages.ByName);

        foreach (var dependency in parser.GetDependencies())
        {
            if (!ignoreByName.Filter(dependency.Name))
            {
                var reference = ReadFromNodeModules(dependency, nodeModulesDirectoryName, false);
                if (reference == null)
                {
                    notFound.Add(new LibraryId(PackageSources.Npm, dependency.Name, dependency.Version));
                }
                else
                {
                    references.Add(reference);
                }
            }
        }

        foreach (var dependency in parser.GetDevDependencies())
        {
            if (!ignoreByName.Filter(dependency.Name))
            {
                var reference = ReadFromNodeModules(dependency, nodeModulesDirectoryName, true);
                if (reference == null)
                {
                    notFound.Add(new LibraryId(PackageSources.Npm, dependency.Name, dependency.Version));
                }
                else
                {
                    references.Add(reference);
                }
            }
        }
    }

    private string GetNpmRoot()
    {
        if (_npmRoot == null)
        {
            _npmRoot = Api.ResolveNpmRoot();
        }

        return _npmRoot;
    }
}