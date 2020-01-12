using System;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal static class NuGetConstants
    {
        public const string RepositorySpecFileName = "package.nuspec";
        public const string RepositoryPackageFileName = "package.nupkg";

        internal static bool IsDeprecateLicenseUrl(string value)
        {
            // https://aka.ms/deprecateLicenseUrl
            if (value.IsNullOrEmpty() || !Uri.TryCreate(value, UriKind.Absolute, out var url))
            {
                return false;
            }

            return url.Host.EqualsIgnoreCase("aka.ms") && url.LocalPath.StartsWithIgnoreCase("/deprecateLicenseUrl");
        }
    }
}
