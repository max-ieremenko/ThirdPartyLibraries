using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic.Internal;

// https://github.com/OpenSourceOrg/api/blob/master/doc/endpoints.md
internal sealed class OpenSourceOrgRepository
{
    public const string Host = "opensource.org";
    public const string ApiHost = "api.opensource.org";

    private readonly Func<HttpClient> _httpClientFactory;

    public OpenSourceOrgRepository(Func<HttpClient> httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    internal OpenSourceOrgIndex? Index { get; set; }

    public async Task LoadIndexAsync(CancellationToken token)
    {
        if (Index != null)
        {
            return;
        }

        JArray? licenses;
        using (var client = _httpClientFactory())
        {
            const string requestUri = "https://" + ApiHost + "/licenses/";
            licenses = await client.GetAsJsonAsync<JArray>(requestUri, token).ConfigureAwait(false);
        }

        Index = licenses == null ? new OpenSourceOrgIndex(0) : OpenSourceOrgIndexParser.Parse(licenses);
    }

    public bool TryFindLicenseCodeByUrl(Uri url, [NotNullWhen(true)] out string? code)
    {
        var index = SafeIndex();

        if (TryParseLicenseCode(url, out var testCode)
            && index.TryGetEntry(testCode, out var entry))
        {
            code = entry.Code;
            return true;
        }

        if (index.TryGetEntry(url, out entry))
        {
            code = entry.Code;
            return true;
        }

        code = null;
        return false;
    }

    public bool TryFindLicenseCode(string code, [NotNullWhen(true)] out string? value)
    {
        if (SafeIndex().TryGetEntry(code, out var entry))
        {
            value = entry.Code;
            return true;
        }

        value = null;
        return false;
    }

    public async Task<LicenseSpec?> TryDownloadByCodeAsync(string code, CancellationToken token)
    {
        if (!SafeIndex().TryGetEntry(code, out var entry))
        {
            return null;
        }

        var result = new LicenseSpec(LicenseSpecSource.Shared, entry.Code) { FullName = entry.FullName };
        if (entry.DownloadUrl == null)
        {
            return result;
        }

        result.HRef = entry.DownloadUrl.ToString();

        using (var client = _httpClientFactory())
        {
            var response = await client.GetFileAsync(result.HRef, token).ConfigureAwait(false);

            if (response.HasValue)
            {
                result.FileContent = response.Value.Content;
                result.FileExtension = response.Value.Extension;
            }
        }

        return result;
    }

    private static bool TryParseLicenseCode(Uri url, [NotNullWhen(true)] out string? code)
    {
        if (!OpenSourceUrlParser.TryParseLicenseCode(url, Host, "license", out var text)
            && !OpenSourceUrlParser.TryParseLicenseCode(url, Host, "licenses", out text)
            && !OpenSourceUrlParser.TryParseLicenseCode(url, ApiHost, "license", out text)
            && !OpenSourceUrlParser.TryParseLicenseCode(url, ApiHost, "licenses", out text))
        {
            code = null;
            return false;
        }

        code = text.ToString();
        return true;
    }

    private OpenSourceOrgIndex SafeIndex()
    {
        var result = Index;
        if (result == null)
        {
            throw new InvalidOperationException("LoadIndexAsync is missing");
        }

        return result;
    }
}