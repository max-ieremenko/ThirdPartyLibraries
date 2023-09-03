using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal interface ICustomPackageUpdater
{
    Task<List<LibraryId>> GetAllCustomLibrariesAsync(CancellationToken token);

    Task UpdateAsync(LibraryId library, CancellationToken token);
}