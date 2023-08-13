using System;
using System.IO;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm.Internal;

internal sealed class NpmPackageSpecParser : IPackageSpecParser
{
    public string RepositorySpecFileName => NpmLibraryId.RepositoryPackageJsonFileName;

    public bool CanParse(LibraryId id) => NpmLibraryId.IsNpm(id);

    public IPackageSpec Parse(Stream specContent) => NpmPackageSpec.FromStream(specContent);

    // packageSource for Npm is always null
    public PackageSource NormalizePackageSource(IPackageSpec spec, string? packageSource)
    {
        var name = Uri.EscapeDataString(spec.GetName());
        var version = Uri.EscapeDataString(spec.GetVersion());
        var relativePath = $"package/{name}/v/{version}";
        
        var downloadUrl = new Uri(new Uri("https://" + NpmHosts.Npm), relativePath);

        return new PackageSource(NpmLibraryId.PackageSource, downloadUrl);
    }
}