using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.GenericAdapters
{
    internal static class PackageRepositoryTools
    {
        public const string RepositoryRemarksFileName = "remarks.md";
        public const string RepositoryThirdPartyNoticesFileName = "third-party-notices.txt";

        public static string BuildUsedBy(IEnumerable<PackageApplication> usedBy)
        {
            usedBy.AssertNotNull(nameof(usedBy));

            var list = usedBy.Select(i => i.InternalOnly ? i.Name + " internal" : i.Name).OrderBy(i => i);
            return string.Join(", ", list);
        }

        public static async Task<string> ReadRemarksFileName(this IStorage storage, LibraryId id, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));

            return await ReadFileAsync(storage, id, RepositoryRemarksFileName, token) ?? "no remarks";
        }

        public static Task<string> ReadThirdPartyNoticesFile(this IStorage storage, LibraryId id, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));

            return ReadFileAsync(storage, id, RepositoryThirdPartyNoticesFileName, token);
        }

        public static Task CreateDefaultRemarksFile(this IStorage storage, LibraryId id, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));

            return CreateEmptyFileAsync(storage, id, RepositoryRemarksFileName, token);
        }

        public static Task CreateDefaultThirdPartyNoticesFile(this IStorage storage, LibraryId id, CancellationToken token)
        {
            storage.AssertNotNull(nameof(storage));

            return CreateEmptyFileAsync(storage, id, RepositoryThirdPartyNoticesFileName, token);
        }

        private static async Task<string> ReadFileAsync(IStorage storage, LibraryId id, string fileName, CancellationToken token)
        {
            string result = null;
            using (var stream = await storage.OpenLibraryFileReadAsync(id, fileName, token))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        result = await reader.ReadToEndAsync();
                    }
                }
            }

            return result.IsNullOrEmpty() ? null : result;
        }

        private static async Task CreateEmptyFileAsync(IStorage storage, LibraryId id, string fileName, CancellationToken token)
        {
            using (var stream = await storage.OpenLibraryFileReadAsync(id, fileName, CancellationToken.None))
            {
                if (stream == null)
                {
                    await storage.WriteLibraryFileAsync(id, fileName, Array.Empty<byte>(), token);
                }
            }
        }
    }
}
