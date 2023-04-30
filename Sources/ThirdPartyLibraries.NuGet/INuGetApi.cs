using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.NuGet;

public interface INuGetApi
{
    Task<byte[]> ExtractSpecAsync(NuGetPackageId package, byte[] packageContent, CancellationToken token);

    NuGetSpec ParseSpec(Stream content);

    Task<string> ResolveLicenseCodeAsync(string licenseUrl, CancellationToken token);

    Task<byte[]> DownloadPackageAsync(NuGetPackageId package, bool allowToUseLocalCache, CancellationToken token);

    Task<byte[]> LoadFileContentAsync(byte[] packageContent, string fileName, CancellationToken token);

    string[] FindFiles(byte[] packageContent, string searchPattern);

    string ResolvePackageSource(NuGetPackageId package);
}