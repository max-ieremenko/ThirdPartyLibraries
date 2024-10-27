using ThirdPartyLibraries.Npm.Internal.Domain;
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
        if (index?.Versions == null)
        {
            return null;
        }

        if (!index.Versions.TryGetValue(version, out var versionEntry)
            || string.IsNullOrEmpty(versionEntry.Dist?.Tarball))
        {
            return null;
        }

        // https://registry.npmjs.org/@types/angular/-/angular-1.6.55.tgz
        var packageUrl = new Uri(versionEntry.Dist.Tarball);

        using (var client = _httpClientFactory())
        using (var stream = await client.GetStreamAsync(packageUrl).ConfigureAwait(false))
        {
            var content = await stream.ToArrayAsync(token).ConfigureAwait(false);
            return content;
        }
    }

    private async Task<NpmPackageIndex?> GetPackageIndexAsync(string packageName, CancellationToken token)
    {
        // does not work: https://registry.npmjs.org/@types%2Fangular/1.6.55
        var url = new Uri(new Uri(Host), Uri.EscapeDataString(packageName));

        using (var client = _httpClientFactory())
        {
            return await client.GetAsJsonAsync(url.ToString(), DomainJsonSerializerContext.Default.NpmPackageIndex, token).ConfigureAwait(false);
        }
    }
}