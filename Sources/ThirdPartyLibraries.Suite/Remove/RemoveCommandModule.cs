using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Suite.Remove.Internal;

namespace ThirdPartyLibraries.Suite.Remove;

internal static class RemoveCommandModule
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IPackageRemover, PackageRemover>();
    }
}