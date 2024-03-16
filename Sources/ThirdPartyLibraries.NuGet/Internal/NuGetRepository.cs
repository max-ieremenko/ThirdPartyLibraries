using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet.Internal;

internal sealed class NuGetRepository : INuGetRepository
{
    public const string Host = "https://" + NuGetHosts.Api;

    private readonly Func<HttpClient> _httpClientFactory;

    public NuGetRepository(Func<HttpClient> httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<byte[]?> TryGetPackageFromCacheAsync(string packageName, string version, List<Uri> sources, CancellationToken token)
    {
        var cache = new NuGetPackageCache(packageName, version);
        var fileName = cache.GetPackageFileName();

        if (!cache.TryFindFile(cache.GetDefaultCachePath(), fileName, out var path))
        {
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source.IsFile() && cache.TryFindFile(source.OriginalString, fileName, out path))
                {
                    break;
                }
            }
        }

        return path == null ? Task.FromResult((byte[]?)null) : File.ReadAllBytesAsync(path, token);
    }

    public async Task<byte[]?> TryDownloadPackageAsync(string packageName, string version, CancellationToken token)
    {
        var url = GetPackageUri(packageName, version);

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
                return await stream.ToArrayAsync(token).ConfigureAwait(false);
            }
        }
    }

    public string? ResolvePackageSource(string packageName, string version, List<Uri> sources)
    {
        var cache = new NuGetPackageCache(packageName, version);
        var fileName = cache.GetMetadataFileName();

        var candidates = new List<Uri>();
        if (cache.TryFindFile(cache.GetDefaultCachePath(), fileName, out var path)
            && NuGetMetadataParser.TryGetSource(path, out var candidate))
        {
            candidates.Add(candidate);
        }

        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            if (source.IsFile()
                && cache.TryFindFile(source.OriginalString, fileName, out path)
                && NuGetMetadataParser.TryGetSource(path, out candidate))
            {
                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        var result = candidates.Find(i => i.IsHttpOrHttps() && i.Host.Contains(NuGetHosts.Api, StringComparison.OrdinalIgnoreCase))
            ?? candidates.Find(UriExtensions.IsHttpOrHttps)
            ?? candidates[0];

        return result.OriginalString;
    }

    private static Uri GetPackageUri(string packageName, string version)
    {
        var name = packageName.ToLowerInvariant();
        var v = version.ToLowerInvariant();
        return new(
            new Uri(Host),
            $"v3-flatcontainer/{name}/{v}/{name}.{v}.nupkg");
    }
}