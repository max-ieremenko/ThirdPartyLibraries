using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.NuGet.Configuration;
using ThirdPartyLibraries.NuGet.Internal;

namespace ThirdPartyLibraries.NuGet;

public static class AppModule
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NuGetConfiguration>(configuration.GetSection(NuGetConfiguration.SectionName));

        services.AddTransient<INuGetRepository, NuGetRepository>();

        services.TryAddEnumerable(ServiceDescriptor.Transient<IPackageReferenceProvider, NuGetPackageReferenceProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IPackageLoaderFactory, NuGetPackageLoaderFactory>());

        services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseByUrlLoader, NuGetLicenseByUrlLoader>());

        services.TryAddEnumerable(ServiceDescriptor.Transient<IPackageSpecParser, NuGetPackageSpecParser>());
    }
}