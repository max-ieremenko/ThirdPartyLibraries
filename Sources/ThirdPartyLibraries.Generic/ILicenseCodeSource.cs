using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Generic;

public interface ILicenseCodeSource
{
    Task<string> ResolveLicenseCodeAsync(string licenseUrl, CancellationToken token);
}