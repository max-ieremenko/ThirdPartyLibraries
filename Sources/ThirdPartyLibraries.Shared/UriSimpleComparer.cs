using System;
using System.Collections.Generic;

namespace ThirdPartyLibraries.Shared;

public sealed class UriSimpleComparer : IEqualityComparer<Uri>
{
    public static readonly UriSimpleComparer Instance = new();

    private UriSimpleComparer()
    {
    }

    public static bool IsSubsetOf(Uri subSet, Uri superSet)
    {
        if (!SchemesEqual(subSet, superSet)
            || !HostsEqual(subSet.Host, superSet.Host)
            || !PathsEqual(subSet, superSet))
        {
            return false;
        }

        return string.IsNullOrEmpty(subSet.Query)
            || QueriesEqual(subSet, superSet);
    }

    public static bool HttpAndHostsEqual(Uri url, string host) => url.IsHttpOrHttps() && HostsEqual(url.Host, host);

    public static bool GetDirectoryName(ReadOnlySpan<char> path, out ReadOnlySpan<char> directory, out ReadOnlySpan<char> rest)
    {
        var cleanPath = path.Trim('/');
        if (cleanPath.IsEmpty)
        {
            directory = default;
            rest = default;
            return false;
        }

        directory = cleanPath;

        var index = cleanPath.IndexOf('/');
        if (index < 0)
        {
            rest = default;
            return true;
        }

        directory = cleanPath.Slice(0, index);
        rest = cleanPath.Slice(index + 1);
        return true;
    }

    public static bool Equals(Uri x, Uri y)
    {
        return SchemesEqual(x, y)
               && HostsEqual(x.Host, y.Host)
               && PathsEqual(x, y)
               && QueriesEqual(x, y);
    }

    bool IEqualityComparer<Uri>.Equals(Uri x, Uri y) => Equals(x, y);

    public int GetHashCode(Uri obj)
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Host),
            GetHashCode(obj.AbsolutePath),
            GetHashCode(obj.Query));
    }

    private static bool SchemesEqual(Uri x, Uri y)
    {
        return x.Scheme.Equals(y.Scheme, StringComparison.OrdinalIgnoreCase)
            || (x.IsHttpOrHttps() && y.IsHttpOrHttps());
    }

    private static bool HostsEqual(string x, string y) => x.Equals(y, StringComparison.OrdinalIgnoreCase);

    private static bool PathsEqual(Uri x, Uri y) => Equal(x.AbsolutePath, y.AbsolutePath);

    private static bool QueriesEqual(Uri x, Uri y) => Equal(x.Query, y.Query);

    private static ReadOnlySpan<char> Trim(in ReadOnlySpan<char> value) => value.Trim('/').TrimStart('?');

    private static bool Equal(in ReadOnlySpan<char> x, in ReadOnlySpan<char> y) => Trim(x).Equals(Trim(y), StringComparison.OrdinalIgnoreCase);

    private static int GetHashCode(string value)
    {
        var hash = Trim(value);
        if (hash.Length == value.Length)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(value);
        }

        return StringComparer.OrdinalIgnoreCase.GetHashCode(hash.ToString());
    }
}