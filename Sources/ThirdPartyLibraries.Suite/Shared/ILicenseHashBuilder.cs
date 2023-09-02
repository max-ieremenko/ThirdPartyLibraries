using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Shared;

internal interface ILicenseHashBuilder
{
    Task<ArrayHash?> GetHashAsync(string licenseCode, string fileName, CancellationToken token);

    Task<(string? FileName, ArrayHash? Hash)> GetHashAsync(LibraryId library, string licenseSubject, CancellationToken token);
}