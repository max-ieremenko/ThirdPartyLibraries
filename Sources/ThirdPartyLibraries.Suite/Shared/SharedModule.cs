using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Suite.Shared.Internal;

namespace ThirdPartyLibraries.Suite.Shared;

internal sealed class SharedModule
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<ISourceCodeParser, SourceCodeParser>();
        services.AddTransient<IPackageSpecLoader, PackageSpecLoader>();

        services.AddSingleton<HttpClientFactory>();
        services.AddSingleton<Func<HttpClient>>(provider => provider.GetRequiredService<HttpClientFactory>().CreateHttpClient);

        services.AddTransient<ILicenseHashBuilder, LicenseHashBuilder>();
    }
}