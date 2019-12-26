using System;
using ThirdPartyLibraries.Npm;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters
{
    internal static class NpmConstants
    {
        public const string RepositoryPackageJsonFileName = "package.json";
        public const string RepositoryPackageFileName = "package.zip";
        public const string RepositoryRemarksFileName = "remarks.md";
        public const string RepositoryThirdPartyNoticesFileName = "third-party-notices.txt";

        internal static Package CreatePackage(PackageJson json, string licenseCode, string licenseStatus)
        {
            var result = new Package
            {
                SourceCode = PackageSources.Npm,
                Name = json.Name,
                Version = json.Version,
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
