using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic;

public static class AppModule
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AssertNotNull(nameof(services));

        services.AddSingleton<IStaticLicenseSource, StaticLicenseSource>();

        services.AddSingleton<OpenSourceOrgApi>();
        services.AddKeyedTransient<ILicenseCodeSource, OpenSourceOrgApi>(
            KnownHosts.OpenSourceOrg,
            provider => provider.GetRequiredService<OpenSourceOrgApi>());
        services.AddKeyedTransient<ILicenseCodeSource, OpenSourceOrgApi>(
            KnownHosts.OpenSourceOrgApi,
            provider => provider.GetRequiredService<OpenSourceOrgApi>());

        services.AddKeyedTransient<ILicenseCodeSource, SpdxOrgApi>(KnownHosts.SpdxOrg);
        services.AddTransient<IFullLicenseSource, SpdxOrgApi>();

        services.AddKeyedTransient<ILicenseCodeSource, CodeProjectApi>(KnownHosts.CodeProject);
        services.AddKeyedTransient<IFullLicenseSource, CodeProjectApi>(CodeProjectApi.LicenseCode);
    }
}