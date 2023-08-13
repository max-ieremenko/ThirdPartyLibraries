using System.Collections.Generic;

namespace ThirdPartyLibraries.Domain;

public interface IPackageReferenceProvider
{
    void AddReferencesFrom(string path, List<IPackageReference> references, HashSet<LibraryId> notFound);
}