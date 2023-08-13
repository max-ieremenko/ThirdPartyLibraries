using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
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

    public async Task<byte[]?> TryGetPackageFromCacheAsync(string packageName, string version, CancellationToken token)
    {
        var path = GetLocalCachePath(packageName, version);
        if (path == null)
        {
            return null;
        }

        var fileName = Path.Combine(path, $"{packageName}.{version}.nupkg".ToLowerInvariant());

        if (!File.Exists(fileName))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fileName, token).ConfigureAwait(false);
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

    public string? ResolvePackageSource(string packageName, string version)
    {
        var cachePath = GetLocalCachePath(packageName, version);
        if (cachePath == null)
        {
            return null;
        }

        var fileName = Path.Combine(cachePath, ".nupkg.metadata");
        if (!File.Exists(fileName))
        {
            return null;
        }

        string result;
        using (var stream = File.OpenRead(fileName))
        {
            result = NuGetMetadataParser.Parse(stream).Source;
        }

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string? GetLocalCachePath(string packageName, string version)
    {
        string path;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            path = Path.Combine(
                Environment.GetEnvironmentVariable("USERPROFILE")!,
                @".nuget\packages",
                packageName,
                version);
        }
        else
        {
            path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @".nuget/packages",
                packageName.ToLowerInvariant(),
                version.ToLowerInvariant());
        }

        return Directory.Exists(path) ? path : null;
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