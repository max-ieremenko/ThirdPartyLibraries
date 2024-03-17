using Microsoft.Extensions.Options;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Generic.Configuration;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic.Internal;

internal sealed class StaticLicenseByUrlLoader : ILicenseByUrlLoader
{
    private readonly List<ConfigurationEntry> _configuration;

    public StaticLicenseByUrlLoader(IOptions<StaticLicenseConfiguration> configuration)
    {
        _configuration = CleanUp(configuration.Value.ByUrl);
    }

    public Task<LicenseSpec?> TryDownloadAsync(Uri url, CancellationToken token)
    {
        return Task.FromResult(TryFind(url));
    }

    private static List<ConfigurationEntry> CleanUp(List<StaticLicenseByUrl> configuration)
    {
        var result = new List<ConfigurationEntry>(configuration.Count);
        for (var i = 0; i < configuration.Count; i++)
        {
            var source = configuration[i];
            if (string.IsNullOrEmpty(source.Code) || source.Urls == null || source.Urls.Length == 0)
            {
                continue;
            }

            var entry = new ConfigurationEntry(source.Code, source.Urls.Length);
            for (var j = 0; j < source.Urls.Length; j++)
            {
                if (Uri.TryCreate(source.Urls[j], UriKind.Absolute, out var url))
                {
                    entry.Urls.Add(url);
                }
            }

            if (entry.Urls.Count > 0)
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private static bool IsMatch(Uri configuration, Uri candidate)
    {
        return UriSimpleComparer.IsSubsetOf(configuration, candidate);
    }

    private LicenseSpec? TryFind(Uri url)
    {
        for (var i = 0; i < _configuration.Count; i++)
        {
            var configuration = _configuration[i];

            for (var j = 0; j < configuration.Urls.Count; j++)
            {
                if (IsMatch(configuration.Urls[j], url))
                {
                    return new LicenseSpec(LicenseSpecSource.Configuration, configuration.Code)
                    {
                        HRef = url.ToString()
                    };
                }
            }
        }

        return null;
    }

    private sealed class ConfigurationEntry
    {
        public ConfigurationEntry(string code, int capacity)
        {
            Code = code;
            Urls = new List<Uri>(capacity);
        }

        public string Code { get; }

        public List<Uri> Urls { get; }
    }
}