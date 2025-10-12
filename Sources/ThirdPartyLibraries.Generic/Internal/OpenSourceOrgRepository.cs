using ThirdPartyLibraries.Generic.Internal.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic.Internal;

// https://opensource.org/blog/introducing-the-new-api-for-osi-approved-licenses
// deprecated: https://github.com/OpenSourceOrg/api/blob/master/doc/endpoints.md
internal sealed class OpenSourceOrgRepository
{
    public const string Host = "opensource.org";
    public const string DeprecatedApiHost = "api.opensource.org";

    private readonly Func<HttpClient> _httpClientFactory;

    public OpenSourceOrgRepository(Func<HttpClient> httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    internal OsiLicenseIndex? Index { get; set; }

    public async Task LoadIndexAsync(CancellationToken token)
    {
        if (Index != null)
        {
            return;
        }

        OsiApprovedLicense[]? licenses;
        using (var client = _httpClientFactory())
        {
            const string requestUri = $"https://{Host}/api/license/";
            licenses = await client.GetAsJsonAsync(requestUri, DomainJsonSerializerContext.Default.OsiApprovedLicenseArray, token).ConfigureAwait(false);
        }

        Index = licenses == null ? new OsiLicenseIndex(0, 0) : OsiLicenseIndexParser.Parse(licenses);
    }

    public bool TryFindLicenseCodeByUrl(Uri url, [NotNullWhen(true)] out string? code)
    {
        var index = SafeIndex();

        if (TryParseLicenseCode(url, out var candidate))
        {
            return index.TryGetCode(candidate, out code);
        }

        return index.TryGetCode(url, out code);
    }

    public bool TryFindLicenseCode(string code, [NotNullWhen(true)] out string? value) =>
        SafeIndex().TryGetCode(code, out value);

    private static bool TryParseLicenseCode(Uri url, [NotNullWhen(true)] out string? code)
    {
        if (!OpenSourceUrlParser.TryParseLicenseCode(url, Host, "license", out var text)
            && !OpenSourceUrlParser.TryParseLicenseCode(url, Host, "licenses", out text)
            && !OpenSourceUrlParser.TryParseLicenseCode(url, DeprecatedApiHost, "license", out text)
            && !OpenSourceUrlParser.TryParseLicenseCode(url, DeprecatedApiHost, "licenses", out text)
            && !OpenSourceUrlParser.TryParseLicenseCode(url, Host, "api", "license", out text)
            && !OpenSourceUrlParser.TryParseLicenseCode(url, Host, "api", "licenses", out text))
        {
            code = null;
            return false;
        }

        code = text.ToString();
        return true;
    }

    private OsiLicenseIndex SafeIndex()
    {
        var result = Index;
        if (result == null)
        {
            throw new InvalidOperationException("LoadIndexAsync is missing");
        }

        return result;
    }
}