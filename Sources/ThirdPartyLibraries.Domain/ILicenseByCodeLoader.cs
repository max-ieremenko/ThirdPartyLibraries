using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Domain;

public interface ILicenseByCodeLoader
{
    Task<LicenseSpec?> TryDownloadAsync(string code, CancellationToken token);
}