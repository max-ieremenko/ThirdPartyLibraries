using ThirdPartyLibraries.Repository.Template;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal interface IStorageLicenseUpdater
{
    Task<LicenseIndexJson?> LoadOrCreateAsync(string licenseCode, CancellationToken token);
}