using System.Net;
using System.Web;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet.Internal;

internal sealed class NuGetLicenseByUrlLoader : ILicenseByUrlLoader
{
    private readonly Func<HttpClient> _httpClientFactory;
    private readonly Dictionary<LicenseCode, LicenseSpec?> _byCode;

    public NuGetLicenseByUrlLoader(Func<HttpClient> httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _byCode = new Dictionary<LicenseCode, LicenseSpec?>();
    }

    public Task<LicenseSpec?> TryDownloadAsync(Uri url, CancellationToken token)
    {
        if (!TryParseCode(url, out var code))
        {
            return Task.FromResult((LicenseSpec?)null);
        }

        return DownloadByCodeAsync(url, code, token);
    }

    private static bool TryParseCode(Uri url, [NotNullWhen(true)] out string? code)
    {
        code = default;
        if (!UriSimpleComparer.HttpAndHostsEqual(url, NuGetHosts.Licenses)
            || !UriSimpleComparer.GetDirectoryName(url.AbsolutePath, out var directory, out var rest)
            || UriSimpleComparer.GetDirectoryName(rest, out _, out _))
        {
            return false;
        }

        code = directory.ToString();
        return true;
    }

    private async Task<LicenseSpec?> DownloadByCodeAsync(Uri url, string code, CancellationToken token)
    {
        var licenseCode = LicenseCode.FromText(HttpUtility.UrlDecode(code));
        if (_byCode.TryGetValue(licenseCode, out var result))
        {
            return result;
        }

        var requestUri = "https://" + NuGetHosts.Licenses + "/" + code;
        var isValid = await IsValidExpressionAsync(requestUri, token).ConfigureAwait(false);
        if (isValid)
        {
            var expression = licenseCode.Codes.Length == 1 ? licenseCode.Codes[0] : licenseCode.Text!;
            result = new LicenseSpec(LicenseSpecSource.Shared, expression) { HRef = url.ToString() };
        }

        _byCode.Add(licenseCode, result);
        return result;
    }

    private async Task<bool> IsValidExpressionAsync(string requestUri, CancellationToken token)
    {
        using (var client = _httpClientFactory())
        using (var response = await client.GetAsync(requestUri, token).ConfigureAwait(false))
        {
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                await response.AssertStatusCodeOk().ConfigureAwait(false);
            }

            return response.IsSuccessStatusCode;
        }
    }
}