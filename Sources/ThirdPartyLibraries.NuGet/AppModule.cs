using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet
{
    public static class AppModule
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AssertNotNull(nameof(services));

            services.AddTransient<INuGetApi, NuGetApi>();
        }
    }
}
