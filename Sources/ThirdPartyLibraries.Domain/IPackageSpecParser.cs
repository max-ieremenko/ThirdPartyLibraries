using System.IO;

namespace ThirdPartyLibraries.Domain;

public interface IPackageSpecParser
{
    string RepositorySpecFileName { get; }

    bool CanParse(LibraryId id);

    IPackageSpec Parse(Stream specContent);

    PackageSource NormalizePackageSource(IPackageSpec spec, string? packageSource);
}