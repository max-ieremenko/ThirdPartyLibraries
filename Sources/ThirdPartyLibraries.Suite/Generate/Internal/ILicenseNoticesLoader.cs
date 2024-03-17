using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal interface ILicenseNoticesLoader
{
    Task<LicenseNotices> LoadAsync(LicenseCode code, CancellationToken token);
}