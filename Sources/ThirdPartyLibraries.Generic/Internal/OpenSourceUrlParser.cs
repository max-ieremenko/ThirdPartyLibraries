using System;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic.Internal;

internal static class OpenSourceUrlParser
{
    public static bool TryParseLicenseCode(
        Uri url,
        string host,
        string directory,
        out ReadOnlySpan<char> code)
    {
        code = default;
        if (!UriSimpleComparer.HttpAndHostsEqual(url, host))
        {
            return false;
        }

        if (!UriSimpleComparer.GetDirectoryName(url.AbsolutePath, out var directory1, out var rest)
            || !directory1.Equals(directory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!UriSimpleComparer.GetDirectoryName(rest, out var directory2, out rest)
            || !rest.IsEmpty)
        {
            return false;
        }

        code = directory2;
        return true;
    }
}