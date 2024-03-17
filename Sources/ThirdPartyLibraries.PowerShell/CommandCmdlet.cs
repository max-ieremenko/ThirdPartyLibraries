using System.Management.Automation;
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
        var program = new ProgramRunAdapter(logger);

        try
        {
            var run = dependencyResolver.BindRunAsync();
            var runTask = run(command, options, program.OnInfo, program.OnWarn, _tokenSource.Token);
            program.Wait(runTask);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }
}