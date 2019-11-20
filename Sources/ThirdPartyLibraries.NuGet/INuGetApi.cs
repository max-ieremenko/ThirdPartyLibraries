using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.NuGet
{
    public interface INuGetApi
    {
        Task<byte[]> LoadSpecAsync(NuGetPackageId package, bool allowToUseLocalCache, CancellationToken token);

        NuGetSpec ParseSpec(Stream content);

        Task<string> ResolveLicenseCodeAsync(string licenseUrl, CancellationToken token);

        Task<byte[]> LoadPackageAsync(NuGetPackageId package, bool allowToUseLocalCache, CancellationToken token);

        Task<byte[]> LoadFileContentAsync(NuGetPackageId package, string fileName, bool allowToUseLocalCache, CancellationToken token);

        Task<NuGetPackageLicenseFile?> TryFindLicenseFileAsync(NuGetPackageId package, bool allowToUseLocalCache, CancellationToken token);
    }
}