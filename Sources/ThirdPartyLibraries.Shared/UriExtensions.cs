using System;

namespace ThirdPartyLibraries.Shared;

public static class UriExtensions
{
    public static bool IsHttp(this Uri uri) => Uri.UriSchemeHttp.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase);

    public static bool IsHttps(this Uri uri) => Uri.UriSchemeHttps.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase);

    public static bool IsFile(this Uri uri) => Uri.UriSchemeFile.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase);

    public static bool IsHttpOrHttps(this Uri uri) => IsHttp(uri) || IsHttps(uri);
}