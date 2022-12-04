using Microsoft.Extensions.Configuration;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite;

namespace ThirdPartyLibraries.Configuration;

internal sealed class ConfigurationManager : IConfigurationManager
{
    public ConfigurationManager(IConfigurationRoot configuration)
    {
        configuration.AssertNotNull(nameof(configuration));

        Configuration = configuration;
    }

    public IConfigurationRoot Configuration { get; }

    public T GetSection<T>(string name)
        where T : new()
    {
        var result = new T();
        Configuration.GetSection(name).Bind(result);
        return result;
    }
}