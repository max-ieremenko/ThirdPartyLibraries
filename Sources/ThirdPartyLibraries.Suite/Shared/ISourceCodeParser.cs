using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Shared;

internal interface ISourceCodeParser
{
    List<IPackageReference> GetReferences(IList<string> locations);
}