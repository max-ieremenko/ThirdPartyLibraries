using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Npm.Internal;

internal static class NpmLibraryId
{
    public const string PackageSource = "npmjs.com";
    public const string RepositoryPackageJsonFileName = "package.json";
    public const string RepositoryPackageFileName = "package.tgz";

    public static bool IsNpm(LibraryId id) => PackageSource.Equals(id.SourceCode, StringComparison.OrdinalIgnoreCase);

    public static LibraryId New(string name, string version) => new(PackageSource, name, version);
}