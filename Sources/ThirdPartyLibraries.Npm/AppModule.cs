using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm
{
    public static class AppModule
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AssertNotNull(nameof(services));

            services.AddTransient<INpmApi, NpmApi>();
        }
    }
}
