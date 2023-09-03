using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Npm.Configuration;
using ThirdPartyLibraries.Npm.Internal;

namespace ThirdPartyLibraries.Npm;

public static class AppModule
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NpmConfiguration>(configuration.GetSection(NpmConfiguration.SectionName));

        services.AddTransient<INpmRegistry, NpmRegistry>();

        services.TryAddEnumerable(ServiceDescriptor.Transient<IPackageReferenceProvider, NpmPackageReferenceProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IPackageLoaderFactory, NpmPackageLoaderFactory>());

        services.TryAddEnumerable(ServiceDescriptor.Transient<IPackageSpecParser, NpmPackageSpecParser>());
    }
}