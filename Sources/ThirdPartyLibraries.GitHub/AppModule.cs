using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.GitHub.Configuration;
using ThirdPartyLibraries.GitHub.Internal;

namespace ThirdPartyLibraries.GitHub;

public static class AppModule
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GitHubConfiguration>(configuration.GetSection(GitHubConfiguration.SectionName));

        services.TryAddEnumerable(ServiceDescriptor.Transient<ILicenseByUrlLoader, GitHubLicenseByUrlLoader>());
        services.AddTransient<IGitHubRepository, GitHubRepository>();
        
        services.TryAddEnumerable(ServiceDescriptor.Transient<INuGetPackageSourceResolver, GitHubNuGetPackageSourceResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IRepositoryNameParser, GitHubRepositoryNameParser>());
    }
}