using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                var fullName = fileName.AsSpan().Slice(root.Length + 1);
                fullName = fullName.Slice(0, fullName.Length - StorageExtensions.IndexFileName.Length - 1);

                var index = fullName.IndexOf(Path.DirectorySeparatorChar);
                var source = fullName.Slice(0, index).ToString();

                fullName = fullName.Slice(index + 1);
                index = fullName.LastIndexOf(Path.DirectorySeparatorChar);
                var version = fullName.Slice(index + 1).ToString();

                var name = fullName.Slice(0, index).ToString().Replace('\\', '/');

                result.Add(new LibraryId(source, name, version));
            }

            return Task.FromResult(result);
        }

        public string GetPackageLocalHRef(LibraryId id, LibraryId? relativeTo = null)
        {
            var connectionString = string.Empty;
            if (relativeTo != null)
            {
                var depth = relativeTo.Value.Name.Count(i => i == '/');
                connectionString = string.Join(string.Empty, Enumerable.Repeat(@".." + Path.DirectorySeparatorChar, depth + 4));
            }

            var href = GetPackageLocation(connectionString, id);
            return href.Replace('\\', '/');
        }

        public string GetLicenseLocalHRef(string licenseCode, LibraryId? relativeTo = null)
        {
            licenseCode.AssertNotNull(nameof(licenseCode));

            var connectionString = string.Empty;
            if (relativeTo != null)
            {
                var depth = relativeTo.Value.Name.Count(i => i == '/');
                connectionString = string.Join(string.Empty, Enumerable.Repeat(@".." + Path.DirectorySeparatorChar, depth + 4));
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
