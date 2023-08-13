using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Suite.Refresh.Internal;

namespace ThirdPartyLibraries.Suite.Refresh;

internal static class RefreshCommandModule
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IPackageReadMeUpdater, PackageReadMeUpdater>();
    }
}