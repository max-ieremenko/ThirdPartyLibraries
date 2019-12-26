using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Tar;

namespace ThirdPartyLibraries.Npm
{
    internal sealed class TarGZip : IAsyncDisposable
    {
        private readonly GZipStream _level1;
        private readonly TarInputStream _tar;

        public TarGZip(byte[] content)
            : this(new MemoryStream(content))
        {
        }

        public TarGZip(Stream content)
        {
            _level1 = new GZipStream(content, CompressionMode.Decompress, leaveOpen: false);
            _tar = new TarInputStream(_level1);
        }

        public bool SeekToEntry(string name)
        {
            TarEntry entry;
            while ((entry = _tar.GetNextEntry()) != null)
            {
                if (entry.IsDirectory || entry.TarHeader.TypeFlag == TarHeader.LF_LINK || entry.TarHeader.TypeFlag == TarHeader.LF_SYMLINK)
                {
                    continue;
                }

                // remove root directory
                var entryName = entry.Name.AsSpan(entry.Name.IndexOf('/') + 1);
                if (entryName.Equals(name.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public byte[] GetCurrentEntryContent()
        {
            using (var copy = new MemoryStream())
            {
                _tar.CopyEntryContents(copy);
                return copy.ToArray();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _level1.DisposeAsync();
            await _tar.DisposeAsync();
        }
    }
}
