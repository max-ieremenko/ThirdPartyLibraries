using System;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Shared.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceProviderKeyedServiceExtensions
{
    public static TService GetKeyedService<TService>(this IServiceProvider provider, string key)
    {
        var collection = provider.GetService<KeyedServiceCollection<TService>>();
        var type = collection?.GetImplementationType(key);
        if (type == null)
        {
            return default;
        }

        return (TService)provider.GetService(type);
    }

    public static TService GetRequiredKeyedService<TService>(this IServiceProvider provider, string key)
    {
        var collection = provider.GetService<KeyedServiceCollection<TService>>();
        var type = collection?.GetImplementationType(key);
        if (type == null)
        {
            throw new InvalidOperationException("Service {0} with key {1} is not registered.".FormatWith(typeof(TService), key));
        }

        return (TService)provider.GetRequiredService(type);
    }
}