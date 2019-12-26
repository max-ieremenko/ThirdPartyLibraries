using ThirdPartyLibraries.Shared;
using Unity;
using Unity.Lifetime;

namespace ThirdPartyLibraries.Npm
{
    public static class AppModule
    {
        public static void ConfigureContainer(IUnityContainer container)
        {
            container.AssertNotNull(nameof(container));

            container.RegisterType<INpmApi, NpmApi>(new TransientLifetimeManager());
        }
    }
}
