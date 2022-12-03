using System;
using System.IO;

namespace ThirdPartyLibraries.Shared;

public static class FileTools
{
    public static string RootPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return Environment.CurrentDirectory;
        }

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));
    }
}