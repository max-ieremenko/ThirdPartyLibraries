using Microsoft.Extensions.Options;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.NuGet.Configuration;

namespace ThirdPartyLibraries.NuGet.Internal;

internal sealed class NuGetPackageLoaderFactory : IPackageLoaderFactory
{
    private readonly INuGetRepository _repository;
    private readonly NuGetConfiguration _configuration;

    public NuGetPackageLoaderFactory(IOptions<NuGetConfiguration> configuration, INuGetRepository repository)
    {
        _repository = repository;
        _configuration = configuration.Value;
    }

    public IPackageLoader? TryCreate(IPackageReference reference)
    {
        if (reference is NuGetPackageReference nuget)
        {
            return new NuGetPackageLoader(nuget, _configuration, _repository);
        }

        return null;
    }
}