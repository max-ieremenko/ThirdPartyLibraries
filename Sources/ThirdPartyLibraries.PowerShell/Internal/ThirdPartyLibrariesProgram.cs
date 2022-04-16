using System;
using System.Management.Automation;
using System.Threading;
using ThirdPartyLibraries.Configuration;

namespace ThirdPartyLibraries.PowerShell.Internal;

internal static class ThirdPartyLibrariesProgram
{
    public static void Run(CommandLine commandLine, Cmdlet cmdlet, CancellationToken token)
    {
        var logger = new CmdLetLogger(cmdlet);

        try
        {
            Program.RunAsync(commandLine, logger, token).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }
}