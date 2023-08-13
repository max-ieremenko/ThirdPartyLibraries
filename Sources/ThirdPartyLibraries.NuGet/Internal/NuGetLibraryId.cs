using System;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.NuGet.Internal;

internal static class NuGetLibraryId
{
    public const string PackageSource = "nuget.org";
    public const string RepositorySpecFileName = "package.nuspec";
    public const string RepositoryPackageFileName = "package.nupkg";

    public static bool IsNuGet(LibraryId id) => PackageSource.Equals(id.SourceCode, StringComparison.OrdinalIgnoreCase);

    public static LibraryId New(string name, string version) => new(PackageSource, name, version);
}