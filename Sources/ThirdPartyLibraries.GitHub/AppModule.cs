using ThirdPartyLibraries.Shared;
using Unity;
using Unity.Lifetime;

namespace ThirdPartyLibraries.GitHub
{
    public static class AppModule
    {
        public static void ConfigureContainer(IUnityContainer container)
        {
            container.AssertNotNull(nameof(container));

            container.RegisterType<IGitHubApi, GitHubApi>(new TransientLifetimeManager());
        }
    }
}
