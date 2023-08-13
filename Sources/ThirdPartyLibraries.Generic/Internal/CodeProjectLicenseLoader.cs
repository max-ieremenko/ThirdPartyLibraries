using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic.Internal;

internal sealed class CodeProjectLicenseLoader : ILicenseByUrlLoader, ILicenseByCodeLoader
{
    public const string LicenseCode = "CPOL";
    private const string Host = "www.codeproject.com";

    private readonly Func<HttpClient> _httpClientFactory;

    public CodeProjectLicenseLoader(Func<HttpClient> httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<LicenseSpec?> TryDownloadAsync(Uri url, CancellationToken token)
    {
        LicenseSpec? result = null;

        if (UriSimpleComparer.HttpAndHostsEqual(url, Host)
            && "/info/cpol10.aspx".Equals(url.AbsolutePath, StringComparison.OrdinalIgnoreCase))
        {
            result = CreateSpec();
        }

        return Task.FromResult(result);
    }

    public async Task<LicenseSpec?> TryDownloadAsync(string code, CancellationToken token)
    {
        if (!LicenseCode.Equals(code, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var result = CreateSpec();

        using (var client = _httpClientFactory())
        using (var response = await client.InvokeGetAsync("https://www.codeproject.com/info/CPOL.zip", token).ConfigureAwait(false))
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await response.AssertStatusCodeOk().ConfigureAwait(false);

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var zip = new ZipArchive(stream))
            {
                var entry = zip.Entries.Count == 1 ? zip.Entries[0] : zip.Entries.First(i => ".htm".Equals(Path.GetExtension(i.Name), StringComparison.OrdinalIgnoreCase));
                using (var entryStream = entry.Open())
                {
                    result.FileContent = await entryStream.ToArrayAsync(token).ConfigureAwait(false);
                    result.FileExtension = ".htm";
                }
            }
        }

        return result;
    }

    private static LicenseSpec CreateSpec() => new(LicenseSpecSource.Shared, LicenseCode)
    {
        FullName = "Code Project Open License (CPOL) 1.02",
        HRef = "https://www.codeproject.com/info/cpol10.aspx"
    };
}