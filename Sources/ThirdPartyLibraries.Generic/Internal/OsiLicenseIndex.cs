using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic.Internal;

internal sealed class OsiLicenseIndex
{
    private readonly Dictionary<string, string> _codeById;
    private readonly Dictionary<string, string> _codeByCode;
    private readonly Dictionary<Uri, string> _codeByUrl;

    public OsiLicenseIndex(int codesCapacity, int urlCapacity)
    {
        _codeById = new(codesCapacity, StringComparer.OrdinalIgnoreCase);
        _codeByCode = new(codesCapacity, StringComparer.OrdinalIgnoreCase);
        _codeByUrl = new(urlCapacity, UriSimpleComparer.Instance);
    }

    public void Add(string id, string spdxId)
    {
        _codeById.Add(id, spdxId);
        _codeByCode.Add(spdxId, spdxId);
    }

    public void Add(string spdxId, Uri url)
    {
        _codeByUrl.Add(url, spdxId);
    }

    public bool TryGetCode(string text, [NotNullWhen(true)] out string? code) =>
        _codeByCode.TryGetValue(text, out code) || _codeById.TryGetValue(text, out code);

    public bool TryGetCode(Uri url, [NotNullWhen(true)] out string? code) =>
        _codeByUrl.TryGetValue(url, out code);
}