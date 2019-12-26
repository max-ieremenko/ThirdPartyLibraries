using System.Net.Http;
using ThirdPartyLibraries.Generic;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;
using ThirdPartyLibraries.Suite.Internal.CustomAdapters;
using ThirdPartyLibraries.Suite.Internal.GitHubAdapters;
using ThirdPartyLibraries.Suite.Internal.NpmAdapters;
using ThirdPartyLibraries.Suite.Internal.NuGetAdapters;
using Unity;
using Unity.Lifetime;

namespace ThirdPartyLibraries.Suite
{
    public static class AppModule
    {
        public static void ConfigureContainer(IUnityContainer container)
        {
            container.RegisterFactory<HttpClient>(_ => HttpClientExtensions.CreateHttpClient(), new TransientLifetimeManager());

            container.RegisterFactory<StaticLicenseConfiguration>(ResolveStaticLicenseConfiguration, new TransientLifetimeManager());
            container.RegisterType<ISourceCodeParser, SourceCodeParser>(new TransientLifetimeManager());
            container.RegisterType<ILicenseResolver, LicenseResolver>(new TransientLifetimeManager());
            container.RegisterType<IPackageRepository, PackageRepository>(new TransientLifetimeManager());

            // nuget
            container.RegisterFactory<NuGetConfiguration>(ResolveNuGetConfiguration, new ContainerControlledLifetimeManager());
            container.RegisterType<ISourceCodeReferenceProvider, NuGetSourceCodeReferenceProvider>(PackageSources.NuGet, new TransientLifetimeManager());
            container.RegisterType<IPackageResolver, NuGetPackageResolver>(PackageSources.NuGet, new TransientLifetimeManager());
            container.RegisterType<ILicenseSourceByUrl, NuGetLicenseSource>(KnownHosts.NuGetLicense, new TransientLifetimeManager());
            container.RegisterType<IPackageRepositoryAdapter, NuGetPackageRepositoryAdapter>(PackageSources.NuGet, new TransientLifetimeManager());

            // npm
            container.RegisterFactory<NpmConfiguration>(ResolveNpmConfiguration, new ContainerControlledLifetimeManager());
            container.RegisterType<ISourceCodeReferenceProvider, NpmSourceCodeReferenceProvider>(PackageSources.Npm, new TransientLifetimeManager());
            container.RegisterType<IPackageRepositoryAdapter, NpmPackageRepositoryAdapter>(PackageSources.Npm, new TransientLifetimeManager());
            container.RegisterType<IPackageResolver, NpmPackageResolver>(PackageSources.Npm, new TransientLifetimeManager());

            // github
            container.RegisterFactory<GitHubConfiguration>(ResolveGitHubConfiguration, new ContainerControlledLifetimeManager());
            container.RegisterType<ILicenseSourceByUrl, GitHubLicenseSource>(KnownHosts.GitHub, new TransientLifetimeManager());
            container.RegisterType<ILicenseSourceByUrl, GitHubLicenseSource>(KnownHosts.GitHubRaw, new TransientLifetimeManager());
            container.RegisterType<ILicenseSourceByUrl, GitHubLicenseSource>(KnownHosts.GitHubApi, new TransientLifetimeManager());

            // custom
            container.RegisterType<IPackageRepositoryAdapter, CustomPackageRepositoryAdapter>(PackageSources.Custom, new TransientLifetimeManager());
        }

        private static StaticLicenseConfiguration ResolveStaticLicenseConfiguration(IUnityContainer container)
        {
            return container
                .Resolve<IConfigurationManager>()
                .GetSection<StaticLicenseConfiguration>(StaticLicenseConfiguration.SectionName);
        }

        private static NuGetConfiguration ResolveNuGetConfiguration(IUnityContainer container)
        {
            return container
                .Resolve<IConfigurationManager>()
                .GetSection<NuGetConfiguration>(PackageSources.NuGet);
        }

        private static NpmConfiguration ResolveNpmConfiguration(IUnityContainer container)
        {
            return container
                .Resolve<IConfigurationManager>()
                .GetSection<NpmConfiguration>(PackageSources.Npm);
        }

        private static GitHubConfiguration ResolveGitHubConfiguration(IUnityContainer container)
        {
            return container
                .Resolve<IConfigurationManager>()
                .GetSection<GitHubConfiguration>(KnownHosts.GitHub);
        }
    }
}
