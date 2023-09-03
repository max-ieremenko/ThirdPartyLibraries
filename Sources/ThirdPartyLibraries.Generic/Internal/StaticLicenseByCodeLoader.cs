using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Generic.Configuration;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic.Internal;

internal sealed class StaticLicenseByCodeLoader : ILicenseByCodeLoader
{
    private readonly Func<HttpClient> _httpClientFactory;
    private readonly Dictionary<string, ConfigurationEntry> _configuration;

    public StaticLicenseByCodeLoader(IOptions<StaticLicenseConfiguration> configuration, Func<HttpClient> httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = CleanUp(configuration.Value.ByCode);
    }

    public async Task<LicenseSpec?> TryDownloadAsync(string code, CancellationToken token)
    {
        if (!_configuration.TryGetValue(code, out var configuration))
        {
            return null;
        }

        if (!configuration.IsProcessed)
        {
            await DownloadAsync(configuration, token).ConfigureAwait(false);
        }

        return new LicenseSpec(LicenseSpecSource.Configuration, configuration.Code)
        {
            FullName = configuration.FullName,
            HRef = configuration.DownloadUrl,
            FileExtension = configuration.FileExtension,
            FileContent = configuration.FileContent
        };
    }

    private static Dictionary<string, ConfigurationEntry> CleanUp(List<StaticLicenseByCode> configuration)
    {
        var result = new Dictionary<string, ConfigurationEntry>(configuration.Count, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < configuration.Count; i++)
        {
            var source = configuration[i];
            if (string.IsNullOrEmpty(source.Code)
                || result.ContainsKey(source.Code)
                || string.IsNullOrEmpty(source.DownloadUrl)
                || !Uri.TryCreate(source.DownloadUrl, UriKind.Absolute, out _))
            {
                continue;
            }

            result.Add(source.Code, new ConfigurationEntry(source.Code, source.FullName, source.DownloadUrl));
        }

        return result;
    }

    private async Task DownloadAsync(ConfigurationEntry configuration, CancellationToken token)
    {
        using (var client = _httpClientFactory())
        {
            var response = await client.GetFileAsync(configuration.DownloadUrl, token).ConfigureAwait(false);

            configuration.IsProcessed = true;
            if (response.HasValue)
            {
                configuration.FileContent = response.Value.Content;
                configuration.FileExtension = response.Value.Extension;
            }
        }
    }

    private sealed class ConfigurationEntry
    {
        public ConfigurationEntry(string code, string? fullName, string downloadUrl)
        {
            Code = code;
            FullName = fullName;
            DownloadUrl = downloadUrl;
        }

        public string Code { get; }

        public string? FullName { get; }

        public string DownloadUrl { get; }

        public bool IsProcessed { get; set; }

        public string? FileExtension { get; set; }

        public byte[]? FileContent { get; set; }
    }
}