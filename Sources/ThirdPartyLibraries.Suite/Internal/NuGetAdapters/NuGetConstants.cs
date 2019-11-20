using System;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal static class NuGetConstants
    {
        public const string RepositorySpecFileName = "package.nuspec";
        public const string RepositoryPackageFileName = "package.nupkg";
        public const string RepositoryRemarksFileName = "remarks.md";
        public const string RepositoryThirdPartyNoticesFileName = "third-party-notices.txt";

        internal static Package CreatePackage(NuGetSpec spec, string licenseCode, string licenseStatus)
        {
            var result = new Package
            {
                SourceCode = PackageSources.NuGet,
                Name = spec.Id,
                Version = spec.Version,
                LicenseCode = licenseCode
            };

            if (!licenseStatus.IsNullOrEmpty())
            {
                result.ApprovalStatus = Enum.Parse<PackageApprovalStatus>(licenseStatus);
            }

            return result;
        }
    }
}
