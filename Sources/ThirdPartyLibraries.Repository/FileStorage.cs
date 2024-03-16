using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Repository;

internal sealed class FileStorage : IStorage
{
    private const string FolderPackages = "packages";
    private const string FolderLicenses = "licenses";
    private const string FolderConfiguration = "configuration";

    private readonly FileSystem<string> _root;
    private readonly FileSystem<string> _configuration;
    private readonly FileSystem<LibraryId> _packages;
    private readonly FileSystem<string> _licenses;

    public FileStorage(string location)
    {
        Location = location;

        _root = new FileSystem<string>(_ => Location, FileMode.Create);
        _configuration = new FileSystem<string>(_ => Path.Combine(Location, FolderConfiguration), FileMode.Create);
        _packages = new FileSystem<LibraryId>(GetPackageLocation, FileMode.Create);
        _licenses = new FileSystem<string>(GetLicenseLocation, FileMode.CreateNew);
    }

    public string Location { get; }

    string IStorage.ConnectionString => Location;

    public Task<List<LibraryId>> GetAllLibrariesAsync(CancellationToken token)
    {
        var result = new List<LibraryId>(0);
        var root = new LibraryPath(Path.Combine(Location, FolderPackages));
        if (!root.Exists)
        {
            return Task.FromResult(result);
        }

        var indexes = root.GetFiles(StorageExtensions.IndexFileName);

        result.Capacity = indexes.Length;
        foreach (var file in indexes)
        {
            result.Add(root.AsLibraryId(file.Directory!));
        }

        result.Sort();
        return Task.FromResult(result);
    }

    public Task<List<string>> GetAllLicenseCodesAsync(CancellationToken token)
    {
        var result = new List<string>(0);
        var root = Path.Combine(Location, FolderLicenses);
        if (!Directory.Exists(root))
        {
            return Task.FromResult(result);
        }

        var directories = Directory.GetDirectories(root);

        result.Capacity = directories.Length;
        foreach (var directoryName in directories)
        {
            var code = Path.GetFileName(directoryName);
            result.Add(code);
        }

        result.Sort();
        return Task.FromResult(result);
    }

    public Task<List<LibraryId>> GetAllLibraryVersionsAsync(string sourceCode, string name, CancellationToken token)
    {
        var result = new List<LibraryId>(0);
        var root = GetPackageLocation(Location, sourceCode, name, null);
        if (!Directory.Exists(root))
        {
            return Task.FromResult(result);
        }

        var versionFolders = Directory.GetDirectories(root);
        result.Capacity = versionFolders.Length;

        for (var i = 0; i < versionFolders.Length; i++)
        {
            var indexFileName = Path.Combine(versionFolders[i], StorageExtensions.IndexFileName);
            if (!File.Exists(indexFileName))
            {
                continue;
            }

            var version = Path.GetFileName(versionFolders[i]);
            result.Add(new LibraryId(sourceCode, name, version));
        }

        result.Sort();
        return Task.FromResult(result);
    }

    public string GetPackageLocalHRef(LibraryId id, LibraryId? relativeTo = null)
    {
        var connectionString = string.Empty;
        if (relativeTo != null)
        {
            var depth = relativeTo.Value.Name.Count(i => i == '/');
            connectionString = string.Join(string.Empty, Enumerable.Repeat(".." + Path.DirectorySeparatorChar, depth + 4));
        }

        var href = GetPackageLocation(connectionString, id.SourceCode, id.Name, id.Version);
        return href.Replace('\\', '/');
    }

    public string GetLicenseLocalHRef(string licenseCode, LibraryId? relativeTo = null)
    {
        var connectionString = string.Empty;
        if (relativeTo != null)
        {
            var depth = relativeTo.Value.Name.Count(i => i == '/');
            connectionString = string.Join(string.Empty, Enumerable.Repeat(@".." + Path.DirectorySeparatorChar, depth + 4));
        }

        var href = GetLicenseLocation(connectionString, licenseCode);
        return href.Replace('\\', '/');
    }

    public Task<Stream?> OpenRootFileReadAsync(string fileName, CancellationToken token)
    {
        return _root.OpenFileReadAsync(null!, fileName, token);
    }

    public Task WriteRootFileAsync(string fileName, byte[] content, CancellationToken token)
    {
        return _root.WriteFileAsync(null!, fileName, content, token);
    }

    public Task<Stream?> OpenConfigurationFileReadAsync(string fileName, CancellationToken token)
    {
        return _configuration.OpenFileReadAsync(null!, fileName, token);
    }

    public Task CreateConfigurationFileAsync(string fileName, byte[] content, CancellationToken token)
    {
        return _configuration.WriteFileAsync(null!, fileName, content, token);
    }

    public Task<Stream?> OpenLibraryFileReadAsync(LibraryId id, string fileName, CancellationToken token)
    {
        return _packages.OpenFileReadAsync(id, fileName, token);
    }

    public Task<string[]> FindLibraryFilesAsync(LibraryId id, string searchPattern, CancellationToken token)
    {
        return _packages.FindFilesAsync(id, searchPattern, token);
    }

    public Task WriteLibraryFileAsync(LibraryId id, string fileName, byte[] content, CancellationToken token)
    {
        return _packages.WriteFileAsync(id, fileName, content, token);
    }

    public Task RemoveLibraryAsync(LibraryId id, CancellationToken token)
    {
        var child = new DirectoryInfo(GetPackageLocation(id));
        if (!child.Exists)
        {
            return Task.CompletedTask;
        }

        new LibraryPath(Path.Combine(Location, FolderPackages)).RemoveLibrary(child);
        return Task.CompletedTask;
    }

    public Task RemoveLibraryFileAsync(LibraryId id, string fileName, CancellationToken token)
    {
        return _packages.RemoveFileAsync(id, fileName, token);
    }

    public Task<Stream?> OpenLicenseFileReadAsync(string licenseCode, string fileName, CancellationToken token)
    {
        return _licenses.OpenFileReadAsync(licenseCode, fileName, token);
    }

    public Task CreateLicenseFileAsync(string licenseCode, string fileName, byte[] content, CancellationToken token)
    {
        return _licenses.WriteFileAsync(licenseCode, fileName, content, token);
    }

    internal string GetPackageLocation(LibraryId id) => GetPackageLocation(Location, id.SourceCode, id.Name, id.Version);

    private static string GetPackageLocation(string connectionString, string sourceCode, string name, string? version)
    {
        var root = Path.Combine(connectionString, FolderPackages, sourceCode.ToLowerInvariant(), name.ToLowerInvariant());
        if (version == null)
        {
            return root;
        }

        return Path.Combine(root, version.ToLowerInvariant());
    }

    private static string GetLicenseLocation(string connectionString, string code)
    {
        return Path.Combine(connectionString, FolderLicenses, code.ToLowerInvariant());
    }

    private string GetLicenseLocation(string code) => GetLicenseLocation(Location, code);
}