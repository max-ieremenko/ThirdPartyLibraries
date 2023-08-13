using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Suite.Generate.Internal;

namespace ThirdPartyLibraries.Suite.Generate;

internal static class GenerateCommandModule
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<ILicenseFileNameResolver, LicenseFileNameResolver>();
        services.AddTransient<ILicenseNoticesLoader, LicenseNoticesLoader>();
        services.AddTransient<IPackageNoticesLoader, PackageNoticesLoader>();
    }
}