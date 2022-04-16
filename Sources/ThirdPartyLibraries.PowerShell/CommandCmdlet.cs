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
        using (new DependencyResolver())
        {
            ProcessRecord(_tokenSource.Token);
        }
    }

    protected sealed override void StopProcessing()
    {
        _tokenSource.Cancel();
        base.StopProcessing();
    }

    protected abstract void ProcessRecord(CancellationToken token);
}