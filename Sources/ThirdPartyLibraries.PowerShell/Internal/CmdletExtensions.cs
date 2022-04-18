using System.IO;
using System.Management.Automation;

namespace ThirdPartyLibraries.PowerShell.Internal
{
    internal static class CmdletExtensions
    {
        public static string GetWorkingDirectory(this PSCmdlet cmdlet)
        {
            var root = cmdlet.MyInvocation.PSScriptRoot;
            if (string.IsNullOrEmpty(root))
            {
                root = cmdlet.CurrentProviderLocation("FileSystem").ProviderPath;
            }

            return root;
        }

        public static string RootPath(this PSCmdlet cmdlet, string path)
        {
            if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
            {
                return path;
            }

            return Path.Combine(GetWorkingDirectory(cmdlet), path);
        }
    }
}
