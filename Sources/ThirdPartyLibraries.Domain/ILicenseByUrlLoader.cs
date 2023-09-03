using System;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Domain;

public interface ILicenseByUrlLoader
{
    Task<LicenseSpec?> TryDownloadAsync(Uri url, CancellationToken token);
}