using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic;

// https://github.com/OpenSourceOrg/api/blob/master/doc/endpoints.md
internal sealed class OpenSourceOrgApi : ILicenseCodeSource
{
    public const string Host = "https://" + KnownHosts.OpenSourceOrgApi;

    private IDictionary<string, string> _spdxIdByCode;

    public OpenSourceOrgApi(Func<HttpClient> httpClientFactory)
    {
        httpClientFactory.AssertNotNull(nameof(httpClientFactory));

        HttpClientFactory = httpClientFactory;
    }

    public Func<HttpClient> HttpClientFactory { get; }
        
    public async Task<string> ResolveLicenseCodeAsync(string licenseUrl, CancellationToken token)
    {
        if (_spdxIdByCode == null)
        {
            _spdxIdByCode = await LoadIndexAsync(token).ConfigureAwait(false);
        }

        if (_spdxIdByCode == null)
        {
            return null;
        }

        var code = GetLicenseCode(licenseUrl);
        _spdxIdByCode.TryGetValue(code, out var result);
        return result;
    }

    private static string GetLicenseCode(string licenseUrl)
    {
        return new Uri(licenseUrl).AbsolutePath.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
    }

    private async Task<IDictionary<string, string>> LoadIndexAsync(CancellationToken token)
    {
        JArray content;
        using (var client = HttpClientFactory())
        {
            content = await client.GetAsJsonAsync<JArray>(Host + "/licenses/", token).ConfigureAwait(false);
        }

        if (content == null)
        {
            return null;
        }

        var spdxIdByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in content)
        {
            var id = item.Value<string>("id");
            var spdxId = item
                .Value<JArray>("identifiers")
                .Where(i => "SPDX".EqualsIgnoreCase(i.Value<string>("scheme")))
                .Select(i => i.Value<string>("identifier"))
                .FirstOrDefault();

            if (spdxId.IsNullOrEmpty())
            {
                spdxIdByCode[id] = id;
            }
            else
            {
                spdxIdByCode[id] = spdxId;
                spdxIdByCode[spdxId] = spdxId;
            }
        }

        return spdxIdByCode;
    }
}