using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic.Internal;

internal sealed class SpdxOrgRepository
{
    public const string Host = "spdx.org";

    private readonly Func<HttpClient> _httpClientFactory;

    // license code in the url is case sensitive: MIT != mit, https://spdx.org/licenses/MIT.json
    private readonly Dictionary<string, LicenseSpec?> _specByCode;

    public SpdxOrgRepository(Func<HttpClient> httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _specByCode = new Dictionary<string, LicenseSpec?>(StringComparer.Ordinal);
    }

    public bool TryParseLicenseCode(Uri url, [NotNullWhen(true)] out string? code)
    {
        if (!OpenSourceUrlParser.TryParseLicenseCode(url, Host, "licenses", out var text))
        {
            code = null;
            return false;
        }

        if (text.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Slice(0, text.Length - 4);
        }
        else if (text.EndsWith(".html", StringComparison.OrdinalIgnoreCase) || text.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Slice(0, text.Length - 5);
        }

        if (text.IsEmpty)
        {
            code = null;
            return false;
        }

        code = text.ToString();
        return true;
    }

    public async Task<LicenseSpec?> TryDownloadByCodeAsync(string code, CancellationToken token)
    {
        if (_specByCode.TryGetValue(code, out var result))
        {
            return result;
        }

        result = await DownloadAsync(code, token).ConfigureAwait(false);
        if (result != null)
        {
            _specByCode.Add(result.Code, result);
        }

        _specByCode.TryAdd(code, result);
        return result;
    }

    private async Task<LicenseSpec?> DownloadAsync(string code, CancellationToken token)
    {
        var href = "https://" + Host + "/licenses/" + code;

        JObject? content;
        using (var client = _httpClientFactory())
        {
            content = await client.GetAsJsonAsync<JObject>(href + ".json", token).ConfigureAwait(false);
        }

        if (content == null)
        {
            return null;
        }

        var licenseId = content.Value<string>("licenseId")!;
        return new LicenseSpec(LicenseSpecSource.Shared, licenseId)
        {
            FullName = content.Value<string>("name")!,
            FileExtension = ".txt",
            FileContent = Encoding.UTF8.GetBytes(content.Value<string>("licenseText")!),
            HRef = href
        };
    }
}