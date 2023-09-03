using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Suite.Configuration;
using ThirdPartyLibraries.Suite.Generate;
using ThirdPartyLibraries.Suite.Refresh;
using ThirdPartyLibraries.Suite.Remove;
using ThirdPartyLibraries.Suite.Shared;
using ThirdPartyLibraries.Suite.Update;
using ThirdPartyLibraries.Suite.Validate;

namespace ThirdPartyLibraries.Suite;

public static class AppModule
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SkipCertificateCheckConfiguration>(configuration.GetSection(SkipCertificateCheckConfiguration.SectionName));
        
        SharedModule.ConfigureServices(services);
        RemoveCommandModule.ConfigureServices(services);
        UpdateCommandModule.ConfigureServices(services);
        ValidateCommandModule.ConfigureServices(services);
        RefreshCommandModule.ConfigureServices(services);
        GenerateCommandModule.ConfigureServices(services);
    }
}