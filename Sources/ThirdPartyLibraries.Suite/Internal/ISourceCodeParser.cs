using System.Collections.Generic;

namespace ThirdPartyLibraries.Suite.Internal;

internal interface ISourceCodeParser
{
    IList<LibraryReference> GetReferences(IList<string> locations);

    IList<LibraryReference> Distinct(IEnumerable<LibraryReference> references);
}