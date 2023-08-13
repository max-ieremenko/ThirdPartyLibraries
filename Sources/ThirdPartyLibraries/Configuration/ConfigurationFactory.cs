using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Configuration;

internal static class ConfigurationFactory
{
    public static async Task<IConfiguration> CreateAsync(
        IServiceCollection services,
        string? repository,
        Dictionary<string, string?> commandLine,
        CancellationToken token)
    {
        var builder = await CreateBuilderAsync(
                services,
                string.IsNullOrWhiteSpace(repository) ? null : StorageFactory.Create(repository),
                token)
            .ConfigureAwait(false);

        var configuration = builder
            .AddUserSecrets(CommandOptions.UserSecretsId, false)
            .AddEnvironmentVariables(prefix: CommandOptions.EnvironmentVariablePrefix)
            .AddInMemoryCollection(commandLine)
            .Build();

        return configuration;
    }

    internal static async Task<ConfigurationBuilder> CreateBuilderAsync(IServiceCollection services, IStorage? storage, CancellationToken token)
    {
        var builder = new ConfigurationBuilder();
        builder.Sources.Clear();

        if (storage == null)
        {
            services.AddSingleton<IStorage>(_ => throw new NotSupportedException("Access to the repository is not supported."));
        }
        else
        {
            services.AddSingleton(storage);

            var settings = await storage.GetOrCreateAppSettingsAsync(token).ConfigureAwait(false);
            builder.AddJsonStream(settings);
        }

        return builder;
    }
}