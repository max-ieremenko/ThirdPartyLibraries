using System;
using System.Diagnostics.CodeAnalysis;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.GitHub.Internal;

internal static class GitHubUrlParser
{
    public const string Host = "github.com";
    public const string HostRaw = "raw.github.com";
    public const string HostRawUserContent = "raw.githubusercontent.com";
    public const string HostApi = "api.github.com";
    public const string HostNuGet = "nuget.pkg.github.com";

    public static bool TryParseRepository(
        Uri url,
        [NotNullWhen(true)] out string? owner,
        [NotNullWhen(true)] out string? repository)
    {
        owner = null;
        repository = null;

        if (!KnownScheme(url))
        {
            return false;
        }

        if (!Host.Equals(url.Host, StringComparison.OrdinalIgnoreCase)
            && !HostRaw.Equals(url.Host, StringComparison.OrdinalIgnoreCase)
            && !HostRawUserContent.Equals(url.Host, StringComparison.OrdinalIgnoreCase)
            && !HostApi.Equals(url.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!UriSimpleComparer.GetDirectoryName(url.AbsolutePath, out var directory1, out var rest)
            || !UriSimpleComparer.GetDirectoryName(rest, out var directory2, out rest))
        {
            return false;
        }

        var spanOwner = directory1;
        var spanRepository = directory2;
        if (url.Host.Equals(HostApi, StringComparison.OrdinalIgnoreCase))
        {
            if (!directory1.Equals("repos", StringComparison.OrdinalIgnoreCase)
                || !UriSimpleComparer.GetDirectoryName(rest, out var directory3, out _))
            {
                return false;
            }

            spanOwner = directory2;
            spanRepository = directory3;
        }

        var path = url.AbsolutePath.AsSpan().Trim('/');

        if (path.Length < 2)
        {
            return false;
        }

        if (spanRepository.EndsWith(".git", StringComparison.OrdinalIgnoreCase) && spanRepository.Length > 4)
        {
            spanRepository = spanRepository.Slice(0, spanRepository.Length - 4);
        }

        owner = spanOwner.ToString();
        repository = spanRepository.ToString();
        return true;
    }

    public static bool TryParseLicenseCode(
        Uri url,
        [NotNullWhen(true)] out string? code)
    {
        code = null;
        if (!UriSimpleComparer.HttpAndHostsEqual(url, HostApi))
        {
            return false;
        }

        if (!UriSimpleComparer.GetDirectoryName(url.AbsolutePath, out var directory1, out var rest)
            || !UriSimpleComparer.GetDirectoryName(rest, out var directory2, out rest)
            || UriSimpleComparer.GetDirectoryName(rest, out _, out _)
            || !directory1.Equals("licenses", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        code = directory2.ToString();
        return true;
    }

    public static bool TryParseNuGetOwner(Uri packageSource, [NotNullWhen(true)] out string? owner)
    {
        owner = null;

        if (!UriSimpleComparer.HttpAndHostsEqual(packageSource, HostNuGet))
        {
            return false;
        }

        if (!UriSimpleComparer.GetDirectoryName(packageSource.AbsolutePath, out var directory1, out var rest))
        {
            return false;
        }

        owner = directory1.ToString();
        return true;
    }

    private static bool KnownScheme(Uri url)
    {
        return Uri.UriSchemeHttp.Equals(url.Scheme, StringComparison.OrdinalIgnoreCase)
               || Uri.UriSchemeHttps.Equals(url.Scheme, StringComparison.OrdinalIgnoreCase)
               || "git".Equals(url.Scheme, StringComparison.OrdinalIgnoreCase);
    }
}