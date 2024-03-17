using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal interface IPackageContentUpdater
{
    Task<UpdateResult> UpdateAsync(IPackageReference reference, string appName, CancellationToken token);
}