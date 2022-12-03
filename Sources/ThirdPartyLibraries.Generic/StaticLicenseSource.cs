using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic;

internal sealed class StaticLicenseSource : IStaticLicenseSource
{
    private readonly IDictionary<string, string> _codeByUrl;
    private readonly IDictionary<string, StaticLicenseByCode> _urlByCode;

    public StaticLicenseSource(StaticLicenseConfiguration configuration, Func<HttpClient> httpClientFactory)
    {
        configuration.AssertNotNull(nameof(configuration));
        httpClientFactory.AssertNotNull(nameof(httpClientFactory));

        HttpClientFactory = httpClientFactory;
        _codeByUrl = CreateCodeByUrl(configuration.ByUrl);
        _urlByCode = CreateUrlByCode(configuration.ByCode);
    }

    public Func<HttpClient> HttpClientFactory { get; }

    public Task<string> ResolveLicenseCodeAsync(string licenseUrl, CancellationToken token)
    {
        licenseUrl.AssertNotNull(nameof(licenseUrl));

        _codeByUrl.TryGetValue(licenseUrl, out var code);
        return Task.FromResult(code);
    }

    public async Task<GenericLicense> DownloadLicenseByCodeAsync(string licenseCode, CancellationToken token)
    {
        licenseCode.AssertNotNull(nameof(licenseCode));

        if (!_urlByCode.TryGetValue(licenseCode, out var configuration))
        {
            return null;
        }

        var result = new GenericLicense
        {
            Code = configuration.Code,
            FullName = configuration.FullName.IsNullOrEmpty() ? configuration.Code : configuration.FullName,
            FileHRef = configuration.DownloadUrl
        };

        if (configuration.DownloadUrl.IsNullOrEmpty())
        {
            return result;
        }

        string mediaType;

        using (var client = HttpClientFactory())
        using (var response = await client.InvokeGetAsync(configuration.DownloadUrl, token).ConfigureAwait(false))
        {
            await response.AssertStatusCodeOk().ConfigureAwait(false);

            mediaType = response.Content.Headers.ContentType.MediaType;

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var content = new MemoryStream())
            {
                await stream.CopyToAsync(content, token).ConfigureAwait(false);
                result.FileContent = content.ToArray();
            }
        }

        if (MediaTypeNames.Text.Html.EqualsIgnoreCase(mediaType))
        {
            result.FileName = "license.html";
        }
        else
        {
            result.FileName = "license.txt";
        }

        return result;
    }

    private static IDictionary<string, string> CreateCodeByUrl(IList<StaticLicenseByUrl> configuration)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in configuration)
        {
            foreach (var url in entry.Urls)
            {
                if (result.ContainsKey(url))
                {
                    throw new InvalidOperationException("Url {0} is duplicated in the appsettings.json/{1}/byUrl".FormatWith(url, StaticLicenseConfiguration.SectionName));
                }

                result.Add(url, entry.Code);
            }
        }

        foreach (var url in result.Keys.ToArray())
        {
            string key;
            if (url.StartsWith(Uri.UriSchemeHttps))
            {
                key = Uri.UriSchemeHttp + url.Substring(Uri.UriSchemeHttps.Length);
            }
            else if (url.StartsWith(Uri.UriSchemeHttp))
            {
                key = Uri.UriSchemeHttps + url.Substring(Uri.UriSchemeHttp.Length);
            }
            else
            {
                continue;
            }

            if (!result.ContainsKey(key))
            {
                result.Add(key, result[url]);
            }
        }

        return result;
    }

    private static IDictionary<string, StaticLicenseByCode> CreateUrlByCode(IList<StaticLicenseByCode> configuration)
    {
        var result = new Dictionary<string, StaticLicenseByCode>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in configuration)
        {
            if (result.ContainsKey(entry.Code))
            {
                throw new InvalidOperationException("Code {0} is duplicated in the appsettings.json/{1}/byCode".FormatWith(entry.Code, StaticLicenseConfiguration.SectionName));
            }

            result.Add(entry.Code, entry);
        }

        return result;
    }
}