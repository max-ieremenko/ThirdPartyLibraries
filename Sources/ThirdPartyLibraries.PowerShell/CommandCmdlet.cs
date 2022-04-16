using System;
using System.Management.Automation;
using System.Threading;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.PowerShell.Internal;

namespace ThirdPartyLibraries.PowerShell;

public abstract class CommandCmdlet : PSCmdlet
{
    private readonly CancellationTokenSource _tokenSource = new();

    protected sealed override void BeginProcessing()
    {
    }

    protected sealed override void EndProcessing()
    {
    }

    protected sealed override void ProcessRecord()
    {
        using (new DependencyResolver())
        {
            RunApp();
        }
    }

    protected sealed override void StopProcessing()
    {
        _tokenSource.Cancel();
        base.StopProcessing();
    }

    protected abstract CommandLine CreateCommandLine();

    private void RunApp()
    {
        var commandLine = CreateCommandLine();

        var logger = new CmdLetLogger(this);

        try
        {
            Program.RunAsync(commandLine, logger, _tokenSource.Token).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }
}