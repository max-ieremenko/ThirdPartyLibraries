using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Shared;

internal interface IPackageSpecLoader
{
    IPackageSpecParser ResolveParser(LibraryId id);

    Task<IPackageSpec?> LoadAsync(LibraryId id, CancellationToken token);
}