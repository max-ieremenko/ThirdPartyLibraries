using System.Collections.Generic;

namespace ThirdPartyLibraries.Domain;

public interface IPackageReference
{
    LibraryId Id { get; }

    // TODO: move to IPackageSpec
    string[] TargetFrameworks { get; }

    // TODO: move to IPackageSpec
    List<LibraryId> Dependencies { get; }

    bool IsInternal { get; }

    IPackageReference UnionWith(IPackageReference other);
}