using System.Text.RegularExpressions;

namespace ThirdPartyLibraries.Npm.Internal;

internal static class NpmPackage
{
    public const string SpecFileName = "package.json";

    public static byte[] ExtractPackageJson(byte[] packageContent)
    {
        var result = LoadFileContent(packageContent, SpecFileName);
        if (result == null)
        {
            throw new InvalidOperationException($"{SpecFileName} not found in the package.");
        }

        return result;
    }

    public static byte[]? LoadFileContent(byte[] packageContent, string fileName)
    {
        using (var zip = new TarGZip(packageContent))
        {
            if (!zip.SeekToEntry(fileName))
            {
                return null;
            }

            return zip.GetCurrentEntryContent();
        }
    }

    public static string[] FindFiles(byte[] packageContent, string searchPattern)
    {
        var pattern = new Regex(searchPattern, RegexOptions.IgnoreCase);

        var result = new List<string>();
        using (var zip = new TarGZip(packageContent))
        {
            foreach (var name in zip.GetFileNames())
            {
                if (pattern.IsMatch(name))
                {
                    result.Add(name);
                }
            }
        }

        return result.ToArray();
    }
}