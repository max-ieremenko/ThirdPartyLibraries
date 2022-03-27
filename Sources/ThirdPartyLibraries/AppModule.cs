using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Suite;
using ThirdPartyLibraries.Suite.Commands;
using ConfigurationManager = ThirdPartyLibraries.Configuration.ConfigurationManager;

namespace ThirdPartyLibraries
{
    internal static class AppModule
    {
        public static async Task AddConfigurationAsync(IServiceCollection services, string repository, CancellationToken token)
        {
            var storage = StorageFactory.Create(repository);
            services.AddSingleton(storage);

            IConfigurationRoot configuration;
            using (var settings = await storage.GetOrCreateAppSettingsAsync(token).ConfigureAwait(false))
            {
                configuration = new ConfigurationBuilder()
                    .AddJsonStream(settings)
                    .AddUserSecrets(CommandFactory.UserSecretsId)
                    .AddEnvironmentVariables(prefix: CommandFactory.EnvironmentVariablePrefix)
                    .Build();
            }

            services.AddSingleton<IConfigurationManager>(new ConfigurationManager(configuration));
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<HelpCommand>();
            services.AddTransient<UpdateCommand>();
            services.AddTransient<RefreshCommand>();
            services.AddTransient<ValidateCommand>();
            services.AddTransient<GenerateCommand>();
        }
    }
}
