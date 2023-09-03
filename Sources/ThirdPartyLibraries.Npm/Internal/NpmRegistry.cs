using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm.Internal;

internal sealed class NpmRegistry : INpmRegistry
{
    public const string Host = "https://" + NpmHosts.Registry;

    private readonly Func<HttpClient> _httpClientFactory;

    public NpmRegistry(Func<HttpClient> httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]?> DownloadPackageAsync(string packageName, string version, CancellationToken token)
    {
        var index = await GetPackageIndexAsync(packageName, token).ConfigureAwait(false);
        if (index == null)
        {
            return null;
        }

        var versionEntry = index.Value<JObject>("versions")!.Value<JObject>(version);
        if (versionEntry == null)
        {
            return null;
        }

        // https://registry.npmjs.org/@types/angular/-/angular-1.6.55.tgz
        var packageUrl = new Uri(versionEntry.Value<JObject>("dist")!.Value<string>("tarball")!);

        using (var client = _httpClientFactory())
        using (var stream = await client.GetStreamAsync(packageUrl).ConfigureAwait(false))
        {
            var content = await stream.ToArrayAsync(token).ConfigureAwait(false);
            return content;
        }
    }

    private async Task<JObject?> GetPackageIndexAsync(string packageName, CancellationToken token)
    {
        // does not work: https://registry.npmjs.org/@types%2Fangular/1.6.55
        var url = new Uri(new Uri(Host), Uri.EscapeDataString(packageName));

        JObject? index;
        using (var client = _httpClientFactory())
        using (var response = await client.GetAsync(url, token).ConfigureAwait(false))
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await response.AssertStatusCodeOk().ConfigureAwait(false);

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                index = stream.JsonDeserialize<JObject>();
            }
        }

        return index;
    }
}