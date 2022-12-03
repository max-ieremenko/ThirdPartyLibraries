using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic;

internal sealed class SpdxOrgApi : ILicenseCodeSource, IFullLicenseSource
{
    public const string Host = "https://" + KnownHosts.SpdxOrg;

    public SpdxOrgApi(Func<HttpClient> httpClientFactory)
    {
        httpClientFactory.AssertNotNull(nameof(httpClientFactory));

        HttpClientFactory = httpClientFactory;
    }

    public Func<HttpClient> HttpClientFactory { get; }

    public async Task<string> ResolveLicenseCodeAsync(string licenseUrl, CancellationToken token)
    {
        var url = GetUrl(licenseUrl);

        var content = await LoadLicenseAsync(url, token).ConfigureAwait(false);
        return content?.Value<string>("licenseId");
    }

    public async Task<GenericLicense> DownloadLicenseByCodeAsync(string licenseCode, CancellationToken token)
    {
        licenseCode.AssertNotNull(nameof(licenseCode));

        var url = Host + "/licenses/" + licenseCode + ".json";
        var content = await LoadLicenseAsync(url, token).ConfigureAwait(false);
        if (content == null)
        {
            return null;
        }

        return new GenericLicense
        {
            Code = content.Value<string>("licenseId"),
            FullName = content.Value<string>("name"),
            FileName = "license.txt",
            FileContent = Encoding.UTF8.GetBytes(content.Value<string>("licenseText")),
            FileHRef = Host + "/licenses/" + licenseCode
        };
    }

    public async Task<JObject> LoadLicenseAsync(string url, CancellationToken token)
    {
        JObject content;
        using (var client = HttpClientFactory())
        {
            content = await client.GetAsJsonAsync<JObject>(url, token).ConfigureAwait(false);
        }

        return content;
    }

    private static string GetUrl(string licenseUrl)
    {
        var code = new Uri(licenseUrl).AbsolutePath.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
        if (code.EndsWithIgnoreCase(".txt")
            || code.EndsWithIgnoreCase(".html")
            || code.EndsWithIgnoreCase(".json"))
        {
            code = Path.GetFileNameWithoutExtension(code);
        }

        return Host + "/licenses/" + code + ".json";
    }
}