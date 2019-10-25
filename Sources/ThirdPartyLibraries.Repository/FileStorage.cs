using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository
{
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
            location.AssertNotNull(nameof(location));

            _root = new FileSystem<string>(_ => Location, FileMode.Create);
            _configuration = new FileSystem<string>(_ => Path.Combine(Location, FolderConfiguration), FileMode.Create);
            _packages = new FileSystem<LibraryId>(GetPackageLocation, FileMode.Create);
            _licenses = new FileSystem<string>(GetLicenseLocation, FileMode.CreateNew);

            Location = location;
        }

        public string Location { get; }

        string IStorage.ConnectionString => Location;

        public Task<IList<LibraryId>> GetAllLibrariesAsync(CancellationToken token)
        {
            var root = Path.Combine(Location, FolderPackages);
            var indexes = Directory.GetFiles(root, StorageExtensions.IndexFileName, SearchOption.AllDirectories);

            IList<LibraryId> result = new List<LibraryId>(indexes.Length);
            foreach (var fileName in indexes)
            {
                var path = Path.GetDirectoryName(fileName);
                var version = Path.GetFileName(path);

                path = Path.GetDirectoryName(path);
                var name = Path.GetFileName(path);

                path = Path.GetDirectoryName(path);
                var source = Path.GetFileName(path);

                result.Add(new LibraryId(source, name, version));
            }

            return Task.FromResult(result);
        }

        public string GetPackageLocalHRef(LibraryId id, RelativeTo relativeTo)
        {
            string connectionString;
            switch (relativeTo)
            {
                case RelativeTo.Library:
                    connectionString = @"..\..\..\..\";
                    break;

                default:
                    connectionString = string.Empty;
                    break;
            }

            var href = GetPackageLocation(connectionString, id);
            return href.Replace('\\', '/');
        }

        public string GetLicenseLocalHRef(string licenseCode, RelativeTo relativeTo)
        {
            string connectionString;
            switch (relativeTo)
            {
                case RelativeTo.Library:
                    connectionString = @"..\..\..\..\";
                    break;

                default:
                    connectionString = string.Empty;
                    break;
            }

            var href = GetLicenseLocation(connectionString, licenseCode);
            return href.Replace('\\', '/');
        }

        public Task<Stream> OpenRootFileReadAsync(string fileName, CancellationToken token)
        {
            return _root.OpenFileReadAsync(null, fileName, token);
        }

        public Task WriteRootFileAsync(string fileName, byte[] content, CancellationToken token)
        {
            return _root.WriteFileAsync(null, fileName, content, token);
        }

        public Task<Stream> OpenConfigurationFileReadAsync(string fileName, CancellationToken token)
        {
            return _configuration.OpenFileReadAsync(null, fileName, token);
        }

        public Task CreateConfigurationFileAsync(string fileName, byte[] content, CancellationToken token)
        {
            return _configuration.WriteFileAsync(null, fileName, content, token);
        }

        public Task<Stream> OpenLibraryFileReadAsync(LibraryId id, string fileName, CancellationToken token)
        {
            return _packages.OpenFileReadAsync(id, fileName, token);
        }

        public Task WriteLibraryFileAsync(LibraryId id, string fileName, byte[] content, CancellationToken token)
        {
            return _packages.WriteFileAsync(id, fileName, content, token);
        }

        public Task RemoveLibraryAsync(LibraryId id, CancellationToken token)
        {
            var location = GetPackageLocation(id);
            if (!Directory.Exists(location))
            {
                return Task.CompletedTask;
            }

            Directory.Delete(location, true);
            
            var parent = Path.GetDirectoryName(location);
            if (Directory.GetDirectories(parent).Length == 0)
            {
                Directory.Delete(parent, true);
            }

            return Task.CompletedTask;
        }

        public Task<Stream> OpenLicenseFileReadAsync(string licenseCode, string fileName, CancellationToken token)
        {
            licenseCode.AssertNotNull(nameof(licenseCode));

            return _licenses.OpenFileReadAsync(licenseCode, fileName, token);
        }

        public Task CreateLicenseFileAsync(string licenseCode, string fileName, byte[] content, CancellationToken token)
        {
            licenseCode.AssertNotNull(nameof(licenseCode));

            return _licenses.WriteFileAsync(licenseCode, fileName, content, token);
        }

        internal string GetPackageLocation(LibraryId id) => GetPackageLocation(Location, id);

        private static string GetPackageLocation(string connectionString, LibraryId id)
        {
            return Path.Combine(connectionString, FolderPackages, id.SourceCode.ToLowerInvariant(), id.Name.ToLowerInvariant(), id.Version.ToLowerInvariant());
        }

        private static string GetLicenseLocation(string connectionString, string code)
        {
            code.AssertNotNull(nameof(code));

            return Path.Combine(connectionString, FolderLicenses, code.ToLowerInvariant());
        }

        private string GetLicenseLocation(string code) => GetLicenseLocation(Location, code);
    }
}
