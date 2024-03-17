using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.NuGet.Internal;

internal sealed class NuGetPackageSpecParser : IPackageSpecParser
{
    private readonly INuGetPackageSourceResolver[] _sourceResolvers;

    public NuGetPackageSpecParser(IEnumerable<INuGetPackageSourceResolver> sourceResolvers)
    {
        _sourceResolvers = sourceResolvers.ToArray();
    }

    public string RepositorySpecFileName => NuGetLibraryId.RepositorySpecFileName;

    public bool CanParse(LibraryId id) => NuGetLibraryId.IsNuGet(id);

    public IPackageSpec Parse(Stream specContent) => NuGetPackageSpec.FromStream(specContent);

    public PackageSource NormalizePackageSource(IPackageSpec spec, string? packageSource)
    {
        if (!string.IsNullOrEmpty(packageSource) && Uri.TryCreate(packageSource, UriKind.Absolute, out var packageSourceUrl))
        {
            for (var i = 0; i < _sourceResolvers.Length; i++)
            {
                if (_sourceResolvers[i].TryResolve(spec, packageSourceUrl, out var result))
                {
                    return result.Value;
                }
            }
        }

        return GetDefaultPackageSource(spec);
    }

    private static PackageSource GetDefaultPackageSource(IPackageSpec spec)
    {
        var name = Uri.EscapeDataString(spec.GetName());
        var version = Uri.EscapeDataString(spec.GetVersion());

        var downloadUrl = "https://" + NuGetHosts.NuGetOrg + "/packages/" + Uri.EscapeDataString(name) + "/" + Uri.EscapeDataString(version);
        return new PackageSource(NuGetLibraryId.PackageSource, new Uri(downloadUrl));
    }
}