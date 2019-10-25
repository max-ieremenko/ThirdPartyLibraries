using System.Collections.Generic;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal interface ISourceCodeReferenceProvider
    {
        IEnumerable<LibraryReference> GetReferencesFrom(string path);
    }
}
