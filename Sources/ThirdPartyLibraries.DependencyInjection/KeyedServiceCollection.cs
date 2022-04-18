using System;
using System.Collections.Generic;

namespace ThirdPartyLibraries.DependencyInjection
{
    public class KeyedServiceCollection<TService>
    {
        private readonly Dictionary<string, Type> _implementationTypeByKey = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        public void Add(string key, Type implementationType)
        {
            _implementationTypeByKey.Add(key, implementationType);
        }

        public Type GetImplementationType(string key)
        {
            _implementationTypeByKey.TryGetValue(key, out var result);
            return result;
        }
    }
}
