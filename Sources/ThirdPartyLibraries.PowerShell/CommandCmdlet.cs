using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
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
        using (var dependencyResolver = new DependencyResolver())
        {
            ProcessRecord(dependencyResolver);
        }
    }

    protected sealed override void StopProcessing()
    {
        _tokenSource.Cancel();
        base.StopProcessing();
    }

    protected abstract string CreateCommandLine(IList<(string Name, string Value)> options);

    private void ProcessRecord(DependencyResolver dependencyResolver)
    {
        var options = new List<(string Name, string Value)>();
        var command = CreateCommandLine(options);

        var logger = new CmdLetLogger(this);

        try
        {
            var run = dependencyResolver.BindRunAsync();
            run(command, options, logger.Info, _tokenSource.Token).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }
}