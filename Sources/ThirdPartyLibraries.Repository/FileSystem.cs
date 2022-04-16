using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository
{
    internal sealed class FileSystem<TId>
    {
        private readonly Func<TId, string> _getLocation;
        private readonly FileMode _fileCreateMode;

        public FileSystem(Func<TId, string> getLocation, FileMode fileCreateMode)
        {
            _getLocation = getLocation;
            _fileCreateMode = fileCreateMode;
        }

        public Task<Stream> OpenFileReadAsync(TId id, string fileName, CancellationToken token)
        {
            fileName.AssertNotNull(nameof(fileName));

            Stream result = null;

            var path = Path.Combine(_getLocation(id), fileName);
            if (File.Exists(path))
            {
                result = File.OpenRead(path);
            }

            return Task.FromResult(result);
        }

        public async Task WriteFileAsync(TId id, string fileName, byte[] content, CancellationToken token)
        {
            fileName.AssertNotNull(nameof(fileName));
            content.AssertNotNull(nameof(content));

            using (var stream = OpenFileWrite(id, fileName))
            {
                await stream.WriteAsync(content, 0, content.Length, token).ConfigureAwait(false);
            }
        }

        private Stream OpenFileWrite(TId id, string fileName)
        {
            var location = _getLocation(id);
            Directory.CreateDirectory(location);

            var path = Path.Combine(location, fileName);
            return new FileStream(path, _fileCreateMode, FileAccess.ReadWrite);
        }
    }
}
