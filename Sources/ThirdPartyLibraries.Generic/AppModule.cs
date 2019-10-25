using ThirdPartyLibraries.Shared;
using Unity;
using Unity.Lifetime;

namespace ThirdPartyLibraries.Generic
{
    public static class AppModule
    {
        public static void ConfigureContainer(IUnityContainer container)
        {
            container.AssertNotNull(nameof(container));

            container.RegisterType<IStaticLicenseSource, StaticLicenseSource>(new ContainerControlledLifetimeManager());

            container.RegisterType<OpenSourceOrgApi>(new ContainerControlledLifetimeManager());
            container.RegisterFactory<ILicenseCodeSource>(KnownHosts.OpenSourceOrg, c => c.Resolve<OpenSourceOrgApi>(), new TransientLifetimeManager());
            container.RegisterFactory<ILicenseCodeSource>(KnownHosts.OpenSourceOrgApi, c => c.Resolve<OpenSourceOrgApi>(), new TransientLifetimeManager());
            
            container.RegisterType<ILicenseCodeSource, SpdxOrgApi>(KnownHosts.SpdxOrg, new TransientLifetimeManager());
            container.RegisterType<IFullLicenseSource, SpdxOrgApi>(new TransientLifetimeManager());
            
            container.RegisterType<ILicenseCodeSource, CodeProjectApi>(KnownHosts.CodeProject, new TransientLifetimeManager());
            container.RegisterType<IFullLicenseSource, CodeProjectApi>(CodeProjectApi.LicenseCode, new TransientLifetimeManager());
        }
    }
}
