using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.NuGet.Configuration;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet.Internal;

internal sealed class NuGetPackageReferenceProvider : IPackageReferenceProvider
{
    private readonly NuGetConfiguration _configuration;

    public NuGetPackageReferenceProvider(IOptions<NuGetConfiguration> configuration)
    {
        _configuration = configuration.Value;
    }

    public void AddReferencesFrom(string path, List<IPackageReference> references, HashSet<LibraryId> notFound)
    {
        if (File.Exists(path)
            && ProjectAssetsParser.FileName.Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
        {
            AddReferencesFromFile(path, references);
        }

        if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, ProjectAssetsParser.FileName, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                AddReferencesFromFile(file, references);
            }
        }
    }

    private void AddReferencesFromFile(string fileName, List<IPackageReference> references)
    {
        var parser = ProjectAssetsParser.FromFile(fileName);

        var targetFrameworks = parser.GetTargetFrameworks();

        var internalFilterByName = new IgnoreFilter(_configuration.InternalPackages.ByName);
        var isInternalByProject = new IgnoreFilter(_configuration.InternalPackages.ByProjectName).Filter(parser.GetProjectName());

        foreach (var entry in GetFilteredReferences(parser, targetFrameworks))
        {
            var isInternal = isInternalByProject || internalFilterByName.Filter(entry.Package.Name);

            var reference = new NuGetPackageReference(
                NuGetLibraryId.New(entry.Package.Name, entry.Package.Version),
                targetFrameworks,
                entry.Dependencies,
                isInternal);
            references.Add(reference);
        }
    }

    private IEnumerable<(LibraryId Package, List<LibraryId> Dependencies)> GetFilteredReferences(ProjectAssetsParser parser, string[] targetFrameworks)
    {
        if (new IgnoreFilter(_configuration.IgnorePackages.ByProjectName).Filter(parser.GetProjectName()))
        {
            yield break;
        }

        var ignoreFilterByName = new IgnoreFilter(_configuration.IgnorePackages.ByName);
        foreach (var targetFramework in targetFrameworks)
        {
            foreach (var entry in parser.GetReferences(targetFramework))
            {
                if (ignoreFilterByName.Filter(entry.Package.Name))
                {
                    continue;
                }

                var dependencies = new List<LibraryId>(entry.Dependencies.Count);
                foreach (var d in entry.Dependencies)
                {
                    if (!ignoreFilterByName.Filter(d.Name))
                    {
                        dependencies.Add(d);
                    }
                }

                yield return (entry.Package, dependencies);
            }
        }
    }
}