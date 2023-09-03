using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Generic.Internal;

internal static class OpenSourceOrgIndexParser
{
    public static OpenSourceOrgIndex Parse(JArray licenses)
    {
        var result = new OpenSourceOrgIndex(licenses.Count);
        foreach (var license in licenses.OfType<JObject>())
        {
            var id = license.Value<string>("id");
            if (!LicenseCode.IsSingleCode(id) || result.TryGetEntry(id, out _))
            {
                continue;
            }

            var entry = new OpenSourceOrgLicenseEntry(id, license.Value<string>("name"));

            if (TryGetArray(license, "links", out var links))
            {
                ParseLinks(links, entry.Urls);
            }

            if (TryGetArray(license, "text", out var text))
            {
                entry.DownloadUrl = ParseText(text, entry.Urls);
            }

            // add after Urls are populated
            result.Add(entry);

            if (TryGetArray(license, "identifiers", out var identifiers))
            {
                foreach (var identifier in identifiers)
                {
                    var code = identifier.Value<string>("identifier");
                    if (!LicenseCode.IsSingleCode(code))
                    {
                        continue;
                    }

                    result.TryAdd(code, entry);
                    if ("SPDX".Equals(identifier.Value<string>("scheme"), StringComparison.OrdinalIgnoreCase))
                    {
                        entry.Code = code;
                    }
                }
            }
        }

        return result;
    }

    private static void ParseLinks(JArray links, HashSet<Uri> urls)
    {
        urls.EnsureCapacity(urls.Count + links.Count);
        foreach (var link in links)
        {
            var url = link.Value<string>("url");
            if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                urls.Add(uri);
            }
        }
    }

    private static Uri? ParseText(JArray text, HashSet<Uri> urls)
    {
        Uri? downloadUrl = null;
        string? downloadUrlMediaType = null;

        urls.EnsureCapacity(urls.Count + text.Count);
        foreach (var link in text)
        {
            var url = link.Value<string>("url");
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                continue;
            }

            urls.Add(uri);

            var mediaType = link.Value<string>("media_type");
            if (SetDownloadUrl(downloadUrl, downloadUrlMediaType, mediaType))
            {
                downloadUrl = uri;
                downloadUrlMediaType = mediaType;
            }
        }

        return downloadUrl;
    }

    private static bool TryGetArray(JObject license, string propertyName, [NotNullWhen(true)] out JArray? array)
    {
        if (!license.TryGetValue(propertyName, out var test)
            || test is not JArray result
            || result.Count == 0)
        {
            array = null;
            return false;
        }

        array = result;
        return true;
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