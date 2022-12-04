using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Repository;

public interface IStorage
{
    string ConnectionString { get; }

    Task<IList<LibraryId>> GetAllLibrariesAsync(CancellationToken token);

    string GetPackageLocalHRef(LibraryId id, LibraryId? relativeTo = null);

    string GetLicenseLocalHRef(string licenseCode, LibraryId? relativeTo = null);

    Task<Stream> OpenRootFileReadAsync(string fileName, CancellationToken token);

    Task WriteRootFileAsync(string fileName, byte[] content, CancellationToken token);

    Task<Stream> OpenConfigurationFileReadAsync(string fileName, CancellationToken token);

    Task CreateConfigurationFileAsync(string fileName, byte[] content, CancellationToken token);

    Task<Stream> OpenLibraryFileReadAsync(LibraryId id, string fileName, CancellationToken token);

    Task WriteLibraryFileAsync(LibraryId id, string fileName, byte[] content, CancellationToken token);

    Task RemoveLibraryAsync(LibraryId id, CancellationToken token);

    Task<string[]> FindLibraryFilesAsync(LibraryId id, string searchPattern, CancellationToken token);

    Task<Stream> OpenLicenseFileReadAsync(string licenseCode, string fileName, CancellationToken token);

    Task CreateLicenseFileAsync(string licenseCode, string fileName, byte[] content, CancellationToken token);
}