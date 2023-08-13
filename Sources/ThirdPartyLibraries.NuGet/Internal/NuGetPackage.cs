using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet.Internal;

internal static class NuGetPackage
{
    public static async Task<byte[]> ExtractSpecAsync(string packageName, byte[] packageContent, CancellationToken token)
    {
        var fileName = $"{packageName}.nuspec";
        var result = await LoadFileContentAsync(packageContent, fileName, token).ConfigureAwait(false);

        if (result == null)
        {
            throw new InvalidOperationException(fileName + " not found in the package.");
        }

        return result;
    }

    public static async Task<byte[]?> LoadFileContentAsync(byte[] packageContent, string fileName, CancellationToken token)
    {
        using (var zip = new ZipArchive(new MemoryStream(packageContent), ZipArchiveMode.Read, false))
        {
            if (!zip.Entries.TryFind(fileName, out var entry))
            {
                return null;
            }

            using (var content = entry.Open())
            {
                return await content.ToArrayAsync(token).ConfigureAwait(false);
            }
        }
    }

    public static string[] FindFiles(byte[] packageContent, string searchPattern)
    {
        var result = new List<string>();

        var pattern = new Regex(searchPattern, RegexOptions.IgnoreCase);
        using (var zip = new ZipArchive(new MemoryStream(packageContent), ZipArchiveMode.Read, false))
        {
            for (var i = 0; i < zip.Entries.Count; i++)
            {
                var entry = zip.Entries[i];
                if (entry.FullName.IndexOf('/') < 0 && pattern.IsMatch(entry.FullName))
                {
                    result.Add(entry.FullName);
                }
            }
        }

        return result.ToArray();
    }

    private static bool TryFind(this ReadOnlyCollection<ZipArchiveEntry> entries, string fileName, [NotNullWhen(true)] out ZipArchiveEntry? entry)
    {
        var entryName = fileName.Replace("\\", "/");
        for (var i = 0; i < entries.Count; i++)
        {
            var test = entries[i];
            if (entryName.Equals(test.FullName, StringComparison.OrdinalIgnoreCase))
            {
                entry = test;
                return true;
            }
        }

        entry = null;
        return false;
    }
}