using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Generic.Configuration;
using ThirdPartyLibraries.Generic.Internal;

namespace ThirdPartyLibraries.Generic;

public static class AppModule
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StaticLicenseConfiguration>(configuration.GetSection(StaticLicenseConfiguration.SectionName));

        services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseByCodeLoader, StaticLicenseByCodeLoader>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseByUrlLoader, StaticLicenseByUrlLoader>());

        services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseByCodeLoader, CodeProjectLicenseLoader>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseByUrlLoader, CodeProjectLicenseLoader>());

        services.AddSingleton<OpenSourceLicenseLoader>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseByUrlLoader, OpenSourceLicenseLoader>(provider => provider.GetRequiredService<OpenSourceLicenseLoader>()));
        services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseByCodeLoader, OpenSourceLicenseLoader>(provider => provider.GetRequiredService<OpenSourceLicenseLoader>()));

        services.AddTransient<SpdxOrgRepository>();
        services.AddTransient<OpenSourceOrgRepository>();
    }
}