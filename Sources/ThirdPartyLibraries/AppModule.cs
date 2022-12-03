using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Suite;
using ThirdPartyLibraries.Suite.Commands;
using ConfigurationManager = ThirdPartyLibraries.Configuration.ConfigurationManager;

namespace ThirdPartyLibraries;

internal static class AppModule
{
    public static async Task AddConfigurationAsync(
        IServiceCollection services,
        string repository,
        Dictionary<string, string> commandLine,
        CancellationToken token)
    {
        var storage = StorageFactory.Create(repository);
        services.AddSingleton(storage);

        IConfigurationRoot configuration;
        using (var settings = await storage.GetOrCreateAppSettingsAsync(token).ConfigureAwait(false))
        {
            var builder = new ConfigurationBuilder();
            builder.Sources.Clear();

            configuration = builder
                .AddJsonStream(settings)
                .AddUserSecrets(CommandOptions.UserSecretsId, false)
                .AddEnvironmentVariables(prefix: CommandOptions.EnvironmentVariablePrefix)
                .AddInMemoryCollection(commandLine)
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