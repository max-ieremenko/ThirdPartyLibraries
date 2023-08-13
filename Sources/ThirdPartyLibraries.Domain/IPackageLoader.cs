using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Domain;

public interface IPackageLoader
{
    string RepositoryPackageFileName { get; }

    string RepositorySpecFileName { get; }

    bool DownloadPackageIntoRepository { get; }
    
    Task<byte[]> DownloadPackageAsync(CancellationToken token);

    Task<byte[]> GetSpecContentAsync(CancellationToken token);

    string? ResolvePackageSource();

    List<PackageSpecLicense> GetLicenses(Stream specContent);

    Task<byte[]?> TryGetFileContentAsync(string fileName, CancellationToken token);

    Task<string[]> FindFilesAsync(string searchPattern, CancellationToken token);
}