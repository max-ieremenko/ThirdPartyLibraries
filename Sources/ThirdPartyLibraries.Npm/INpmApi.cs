using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Npm
{
    public interface INpmApi
    {
        PackageJson ParsePackageJson(Stream content);
        
        Task<NpmPackageFile?> DownloadPackageAsync(NpmPackageId id, CancellationToken token);

        Task<byte[]> ExtractPackageJsonAsync(byte[] packageContent, CancellationToken token);
        
        Task<byte[]> LoadFileContentAsync(byte[] packageContent, string fileName, CancellationToken token);
    }
}
