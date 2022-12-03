using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Npm;

public interface INpmApi
{
    PackageJson ParsePackageJson(Stream content);
        
    Task<NpmPackageFile?> DownloadPackageAsync(NpmPackageId id, CancellationToken token);

    byte[] ExtractPackageJson(byte[] packageContent);
        
    byte[] LoadFileContent(byte[] packageContent, string fileName);

    string[] FindFiles(byte[] packageContent, string searchPattern);

    string ResolveNpmRoot();
}