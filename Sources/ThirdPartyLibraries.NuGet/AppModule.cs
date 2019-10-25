using ThirdPartyLibraries.Shared;
using Unity;
using Unity.Lifetime;

namespace ThirdPartyLibraries.NuGet
{
    public static class AppModule
    {
        public static void ConfigureContainer(IUnityContainer container)
        {
            container.AssertNotNull(nameof(container));

            container.RegisterType<INuGetApi, NuGetApi>(new TransientLifetimeManager());
        }
    }
}
