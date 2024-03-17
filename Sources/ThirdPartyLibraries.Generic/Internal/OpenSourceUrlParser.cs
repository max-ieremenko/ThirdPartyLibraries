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

        code = RemoveEnding(directory2);
        return code.Length > 0;
    }

    private static ReadOnlySpan<char> RemoveEnding(ReadOnlySpan<char> code)
    {
        const string Ending1 = "-license.php";
        const string Ending2 = "-license";
        
        if (code.EndsWith(Ending1, StringComparison.OrdinalIgnoreCase))
        {
            return code.Slice(0, code.Length - Ending1.Length);
        }

        if (code.EndsWith(Ending2, StringComparison.OrdinalIgnoreCase))
        {
            return code.Slice(0, code.Length - Ending2.Length);
        }

        return code;
    }
}