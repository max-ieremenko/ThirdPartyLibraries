using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Generic.Internal.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic.Internal;

internal static class OsiLicenseIndexParser
{
    public static OsiLicenseIndex Parse(OsiApprovedLicense[] licenses)
    {
        var byId = new Dictionary<string, OsiApprovedLicense?[]>(licenses.Length, StringComparer.OrdinalIgnoreCase);
        var byCode = new Dictionary<string, OsiApprovedLicense?[]>(licenses.Length, StringComparer.OrdinalIgnoreCase);
        var byUrl = new Dictionary<Uri, OsiApprovedLicense?[]>(licenses.Length, UriSimpleComparer.Instance);

        foreach (var license in licenses)
        {
            if (string.IsNullOrEmpty(license.Id) || string.IsNullOrEmpty(license.SpdxId) || !LicenseCode.IsSingleCode(license.SpdxId))
            {
                continue;
            }

            byId.Add(license.Id, license);
            byCode.Add(license.SpdxId, license);

            if (!string.IsNullOrEmpty(license.LicenseStewardUrl) && Uri.TryCreate(license.LicenseStewardUrl, UriKind.Absolute, out var url))
            {
                byUrl.Add(url, license);
            }
        }

        var result = new OsiLicenseIndex(byCode.Count, byUrl.Count);
        foreach (var entries in byCode.Values)
        {
            var candidate = entries[0]!;
            if (entries[1] != null || byId[candidate.Id][1] != null)
            {
                continue;
            }

            result.Add(candidate.Id, candidate.SpdxId!);
        }

        foreach (var (url, entries) in byUrl)
        {
            var candidate = entries[0]!;
            if (entries[1] != null || byId[candidate.Id][1] != null || byCode[candidate.SpdxId!][1] != null)
            {
                continue;
            }

            result.Add(candidate.SpdxId!, url);
        }

        return result;
    }

    private static void Add<TKey>(this Dictionary<TKey, OsiApprovedLicense?[]> target, TKey key, OsiApprovedLicense value)
    {
        if (!target.TryGetValue(key, out var values))
        {
            values = new OsiApprovedLicense[2];
            target.Add(key, values);
        }

        values[values[0] == null ? 0 : 1] = value;
    }
}