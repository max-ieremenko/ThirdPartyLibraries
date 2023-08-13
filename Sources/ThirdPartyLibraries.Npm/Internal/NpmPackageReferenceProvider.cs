using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Npm.Configuration;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm.Internal;

internal sealed class NpmPackageReferenceProvider : IPackageReferenceProvider
{
    private readonly NpmConfiguration _configuration;
    private string? _npmRoot;

    public NpmPackageReferenceProvider(IOptions<NpmConfiguration> configuration)
    {
        _configuration = configuration.Value;
    }

    public void AddReferencesFrom(string path, List<IPackageReference> references, HashSet<LibraryId> notFound)
    {
        if (File.Exists(path)
            && NpmPackage.SpecFileName.Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
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
        var fileName = Path.Combine(path, NpmPackage.SpecFileName);
        if (File.Exists(fileName))
        {
            // project root
            return new[] { fileName };
        }

        return Directory.GetDirectories(path).SelectMany(FindAllPackageJson);
    }

    private NpmPackageReference? ReadFromNodeModules(
        LibraryId dependency,
        string nodeModulesDirectoryName,
        bool isInternal)
    {
        var fileName = Path.Combine(nodeModulesDirectoryName, dependency.Name, NpmPackage.SpecFileName);
        if (!File.Exists(fileName))
        {
            fileName = Path.Combine(GetNpmRoot(), dependency.Name, NpmPackage.SpecFileName);
            if (!File.Exists(fileName))
            {
                // throw new FileNotFoundException("File {0} not found.".FormatWith(fileName));
                return null;
            }
        }

        var spec = NpmPackageSpec.FromFile(fileName);

        return new NpmPackageReference(
            NpmLibraryId.New(spec.GetName(), spec.GetVersion()),
            isInternal);
    }

    private void AddReferencesFromFile(
        string fileName,
        List<IPackageReference> references,
        HashSet<LibraryId> notFound)
    {
        var folderName = Path.GetFileName(Path.GetDirectoryName(fileName));
        if (new IgnoreFilter(_configuration.IgnorePackages.ByFolderName).Filter(folderName!))
        {
            return;
        }

        var nodeModulesDirectoryName = Path.Combine(Path.GetDirectoryName(fileName)!, NpmRoot.NodeModules);
        if (!Directory.Exists(nodeModulesDirectoryName))
        {
            throw new DirectoryNotFoundException($"Directory {nodeModulesDirectoryName} not found. Did you run \"npm install\"?");
        }

        var spec = NpmPackageSpec.FromFile(fileName);
        var ignoreByName = new IgnoreFilter(_configuration.IgnorePackages.ByName);

        foreach (var dependency in spec.GetDependencies())
        {
            if (!ignoreByName.Filter(dependency.Name))
            {
                var reference = ReadFromNodeModules(dependency, nodeModulesDirectoryName, false);
                if (reference == null)
                {
                    notFound.Add(dependency);
                }
                else
                {
                    references.Add(reference);
                }
            }
        }

        foreach (var dependency in spec.GetDevDependencies())
        {
            if (!ignoreByName.Filter(dependency.Name))
            {
                var reference = ReadFromNodeModules(dependency, nodeModulesDirectoryName, true);
                if (reference == null)
                {
                    notFound.Add(NpmLibraryId.New(dependency.Name, dependency.Version));
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
            _npmRoot = NpmRoot.Resolve();
        }

        return _npmRoot;
    }
}