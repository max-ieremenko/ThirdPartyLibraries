using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ThirdPartyLibraries.Npm.Internal;

internal static class NpmRoot
{
    public const string NodeModules = "node_modules";

    public static string Resolve()
    {
        var info = new ProcessStartInfo
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            ErrorDialog = false,
            Environment =
            {
                { "npm_config_loglevel", "silent" }
            }
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            info.FileName = "cmd";
            info.Arguments = "/c \"npm root -g\"";
            info.LoadUserProfile = true;
        }
        else
        {
            info.FileName = "npm";
            info.Arguments = "root -g";
        }

        Process process;
        try
        {
            process = Process.Start(info)!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fail to execute command [npm root -g]: {ex.Message}", ex);
        }

        string result;
        using (process)
        {
            process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
            result = process.StandardOutput.ReadToEnd().Trim('\r', '\n');

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"The command [npm root -g] exited with code {process.ExitCode}.");
            }
        }

        ////if (!Directory.Exists(result))
        ////{
        ////    throw new DirectoryNotFoundException(string.Format("Npm root directory {0} not found.", result));
        ////}

        return result;
    }
}