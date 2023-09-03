using Microsoft.Extensions.Options;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Npm.Configuration;

namespace ThirdPartyLibraries.Npm.Internal;

internal sealed class NpmPackageLoaderFactory : IPackageLoaderFactory
{
    private readonly INpmRegistry _registry;
    private readonly NpmConfiguration _configuration;

    public NpmPackageLoaderFactory(IOptions<NpmConfiguration> configuration, INpmRegistry registry)
    {
        _registry = registry;
        _configuration = configuration.Value;
    }

    public IPackageLoader? TryCreate(IPackageReference reference)
    {
        if (reference is NpmPackageReference npm)
        {
            return new NpmPackageLoader(npm, _configuration, _registry);
        }

        return null;
    }
}