using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Generic;

public interface IFullLicenseSource
{
    Task<GenericLicense> DownloadLicenseByCodeAsync(string licenseCode, CancellationToken token);
}