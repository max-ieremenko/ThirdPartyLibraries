using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Generic.Internal.Domain;

namespace ThirdPartyLibraries.Generic.Internal;

internal static class OpenSourceOrgIndexParser
{
    public static OpenSourceOrgIndex Parse(OpenSourceOrgLicense[] licenses)
    {
        var result = new OpenSourceOrgIndex(licenses.Length);
        foreach (var license in licenses)
        {
            if (!LicenseCode.IsSingleCode(license.Id) || result.TryGetEntry(license.Id, out _))
            {
                continue;
            }

            var entry = new OpenSourceOrgLicenseEntry(license.Id, license.Name);

            if (license.Links?.Length > 0)
            {
                ParseLinks(license.Links, entry.Urls);
            }

            if (license.Text?.Length > 0)
            {
                entry.DownloadUrl = ParseText(license.Text, entry.Urls);
            }

            // add after Urls are populated
            result.Add(entry);

            if (license.Identifiers?.Length > 0)
            {
                foreach (var identifier in license.Identifiers)
                {
                    var code = identifier.Identifier;
                    if (!LicenseCode.IsSingleCode(code))
                    {
                        continue;
                    }

                    result.TryAdd(code, entry);
                    if ("SPDX".Equals(identifier.Scheme, StringComparison.OrdinalIgnoreCase))
                    {
                        entry.Code = code;
                    }
                }
            }
        }

        return result;
    }

    private static void ParseLinks(OpenSourceOrgLicenseLink[] links, HashSet<Uri> urls)
    {
        urls.EnsureCapacity(urls.Count + links.Length);
        foreach (var link in links)
        {
            var url = link.Url;
            if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                urls.Add(uri);
            }
        }
    }

    private static Uri? ParseText(OpenSourceOrgLicenseText[] text, HashSet<Uri> urls)
    {
        Uri? downloadUrl = null;
        string? downloadUrlMediaType = null;

        urls.EnsureCapacity(urls.Count + text.Length);
        foreach (var link in text)
        {
            var url = link.Url;
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                continue;
            }

            urls.Add(uri);

            var mediaType = link.MediaType;
            if (SetDownloadUrl(downloadUrl, downloadUrlMediaType, mediaType))
            {
                downloadUrl = uri;
                downloadUrlMediaType = mediaType;
            }
        }

        return downloadUrl;
    }

    private static bool SetDownloadUrl(Uri? current, string? currentMediaType, string? candidateMediaType)
    {
        if (current == null)
        {
            return true;
        }

        if ("text/plain".Equals(currentMediaType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if ("text/plain".Equals(candidateMediaType, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !"text/html".Equals(currentMediaType, StringComparison.OrdinalIgnoreCase);
    }
}