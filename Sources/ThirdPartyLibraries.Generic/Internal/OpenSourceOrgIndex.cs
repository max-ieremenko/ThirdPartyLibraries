using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ThirdPartyLibraries.Generic.Internal;

internal sealed class OpenSourceOrgIndex
{
    private readonly Dictionary<string, OpenSourceOrgLicenseEntry> _byCode;
    private readonly HashSet<string> _knownHosts;

    public OpenSourceOrgIndex(int capacity)
    {
        _byCode = new Dictionary<string, OpenSourceOrgLicenseEntry>(capacity, StringComparer.OrdinalIgnoreCase);
        _knownHosts = new HashSet<string>(0, StringComparer.OrdinalIgnoreCase);
    }

    public void Add(OpenSourceOrgLicenseEntry entry)
    {
        _byCode.Add(entry.Code, entry);

        foreach (var url in entry.Urls)
        {
            _knownHosts.Add(url.Host);
        }
    }

    public void TryAdd(string newCode, OpenSourceOrgLicenseEntry entry)
    {
        _byCode.TryAdd(newCode, entry);
    }

    public bool TryGetEntry(string code, [NotNullWhen(true)] out OpenSourceOrgLicenseEntry? entry)
    {
        return _byCode.TryGetValue(code, out entry);
    }

    public bool TryGetEntry(Uri url, [NotNullWhen(true)] out OpenSourceOrgLicenseEntry? entry)
    {
        entry = null;
        if (!_knownHosts.Contains(url.Host))
        {
            return false;
        }

        foreach (var testEntry in _byCode.Values)
        {
            if (testEntry.Urls.Contains(url))
            {
                entry = testEntry;
                return true;
            }
        }

        return false;
    }
}