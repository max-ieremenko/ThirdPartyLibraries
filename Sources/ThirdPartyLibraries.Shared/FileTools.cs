using System;
using System.IO;

namespace ThirdPartyLibraries.Shared
{
    public static class FileTools
    {
        public static string RootPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }

            if (Path.IsPathRooted(path))
            {
                return path;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }
    }
}
