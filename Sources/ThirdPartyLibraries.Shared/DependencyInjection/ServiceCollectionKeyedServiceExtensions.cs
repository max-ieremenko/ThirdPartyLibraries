using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThirdPartyLibraries.Shared.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionKeyedServiceExtensions
{
    public static IServiceCollection AddKeyedTransient<TService, TImplementation>(
        this IServiceCollection services,
        string key,
        Func<IServiceProvider, TImplementation> implementationFactory = null)
        where TService : class
        where TImplementation : class, TService
    {
        var collection = GetOrAddKeyedServiceCollection<TService>(services);
        collection.Add(key, typeof(TImplementation));

        if (implementationFactory == null)
        {
            services.TryAddTransient(typeof(TImplementation));
        }
        else
        {
            services.TryAddTransient(typeof(TImplementation), implementationFactory);
        }

        return services;
    }

    private static KeyedServiceCollection<TService> GetOrAddKeyedServiceCollection<TService>(IServiceCollection services)
    {
        var count = services.Count;
        var type = typeof(KeyedServiceCollection<TService>);
        for (var i = 0; i < count; i++)
        {
            var descriptor = services[i];
            if (descriptor.ServiceType == type)
            {
                return (KeyedServiceCollection<TService>)descriptor.ImplementationInstance;
            }
        }

        var result = new KeyedServiceCollection<TService>();
        services.AddSingleton(result);
        return result;
    }
}