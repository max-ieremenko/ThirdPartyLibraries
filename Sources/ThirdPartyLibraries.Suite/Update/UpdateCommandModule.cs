using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Suite.Update.Internal;

namespace ThirdPartyLibraries.Suite.Update;

internal static class UpdateCommandModule
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILicenseByCodeResolver, LicenseByCodeResolver>();
        services.AddSingleton<ILicenseByUrlResolver, LicenseByUrlResolver>();
        services.AddSingleton<IPackageContentUpdater, PackageContentUpdater>();
        services.AddSingleton<IPackageLicenseUpdater, PackageLicenseUpdater>();
        services.AddSingleton<IStorageLicenseUpdater, StorageLicenseUpdater>();
        services.AddTransient<ICustomPackageUpdater, CustomPackageUpdater>();
    }
}