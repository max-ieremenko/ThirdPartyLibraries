using System.Collections.Generic;
using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Suite.Internal;

internal interface ISourceCodeReferenceProvider
{
    void AddReferencesFrom(string path, IList<LibraryReference> references, ICollection<LibraryId> notFound);
}