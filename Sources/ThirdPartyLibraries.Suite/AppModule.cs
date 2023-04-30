using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThirdPartyLibraries.Generic;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;
using ThirdPartyLibraries.Suite.Internal.CustomAdapters;
using ThirdPartyLibraries.Suite.Internal.GitHubAdapters;
using ThirdPartyLibraries.Suite.Internal.NpmAdapters;
using ThirdPartyLibraries.Suite.Internal.NuGetAdapters;

namespace ThirdPartyLibraries.Suite;

public static class AppModule
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AssertNotNull(nameof(services));

        services.AddSingleton(ResolveHttpClientFactory);

        services.AddSingleton(ResolveStaticLicenseConfiguration);
        services.AddTransient<ISourceCodeParser, SourceCodeParser>();
        services.AddSingleton<ILicenseCache, LicenseCache>();
        services.AddTransient<ILicenseResolver, LicenseResolver>();
        services.AddTransient<IPackageRepository, PackageRepository>();

        // nuget
        services.AddSingleton(ResolveNuGetConfiguration);
        services.TryAddEnumerable(ServiceDescriptor.Transient<ISourceCodeReferenceProvider, NuGetSourceCodeReferenceProvider>());
        services.AddKeyedTransient<IPackageResolver, NuGetPackageResolver>(PackageSources.NuGet);
        services.AddKeyedTransient<ILicenseSourceByUrl, NuGetLicenseSource>(KnownHosts.NuGetLicense);
        services.AddKeyedTransient<IPackageRepositoryAdapter, NuGetPackageRepositoryAdapter>(PackageSources.NuGet);
        services.AddKeyedTransient<INuGetPackageUrlResolver, DefaultNuGetPackageUrlResolver>(KnownHosts.NuGetApi);

        // npm
        services.AddSingleton(ResolveNpmConfiguration);
        services.TryAddEnumerable(ServiceDescriptor.Transient<ISourceCodeReferenceProvider, NpmSourceCodeReferenceProvider>());
        services.AddKeyedTransient<IPackageRepositoryAdapter, NpmPackageRepositoryAdapter>(PackageSources.Npm);
        services.AddKeyedTransient<IPackageResolver, NpmPackageResolver>(PackageSources.Npm);

        // github
        services.AddSingleton(ResolveGitHubConfiguration);
        services.AddKeyedTransient<ILicenseSourceByUrl, GitHubLicenseSource>(KnownHosts.GitHub);
        services.AddKeyedTransient<ILicenseSourceByUrl, GitHubLicenseSource>(KnownHosts.GitHubRaw);
        services.AddKeyedTransient<ILicenseSourceByUrl, GitHubLicenseSource>(KnownHosts.GitHubRawUserContent);
        services.AddKeyedTransient<ILicenseSourceByUrl, GitHubLicenseSource>(KnownHosts.GitHubApi);
        services.AddKeyedTransient<INuGetPackageUrlResolver, GitHubNuGetPackageUrlResolver>(KnownHosts.GitHubNuGet);

        // custom
        services.AddKeyedTransient<IPackageRepositoryAdapter, CustomPackageRepositoryAdapter>(PackageSources.Custom);
    }

    private static StaticLicenseConfiguration ResolveStaticLicenseConfiguration(IServiceProvider provider)
    {
        return provider
            .GetRequiredService<IConfigurationManager>()
            .GetSection<StaticLicenseConfiguration>(StaticLicenseConfiguration.SectionName);
    }

    private static NuGetConfiguration ResolveNuGetConfiguration(IServiceProvider provider)
    {
        return provider
            .GetRequiredService<IConfigurationManager>()
            .GetSection<NuGetConfiguration>(PackageSources.NuGet);
    }

    private static NpmConfiguration ResolveNpmConfiguration(IServiceProvider provider)
    {
        return provider
            .GetRequiredService<IConfigurationManager>()
            .GetSection<NpmConfiguration>(PackageSources.Npm);
    }

    private static GitHubConfiguration ResolveGitHubConfiguration(IServiceProvider provider)
    {
        return provider
            .GetRequiredService<IConfigurationManager>()
            .GetSection<GitHubConfiguration>(KnownHosts.GitHub);
    }

    private static Func<HttpClient> ResolveHttpClientFactory(IServiceProvider provider)
    {
        var configuration = provider
            .GetRequiredService<IConfigurationManager>()
            .GetSection<SkipCertificateCheckConfiguration>(SkipCertificateCheckConfiguration.SectionName);

        var factory = new HttpClientFactory(configuration);

        return factory.CreateHttpClient;
    }
}