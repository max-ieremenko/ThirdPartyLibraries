using System;
using System.Management.Automation;

namespace ThirdPartyLibraries.PowerShell.Internal;

internal sealed class CmdLetLogger : ICmdLetLogger
{
    private readonly Cmdlet _cmdlet;

    public CmdLetLogger(Cmdlet cmdlet)
    {
        _cmdlet = cmdlet;
    }

    public void Error(Exception exception)
    {
        _cmdlet.WriteVerbose(exception.ToString());

        _cmdlet.WriteError(new ErrorRecord(
            exception,
            null,
            ErrorCategory.NotSpecified,
            null));
    }

    public void Info(string message)
    {
        _cmdlet.WriteInformation(new InformationRecord(message, null));
    }

    public void Warn(string message)
    {
        _cmdlet.WriteWarning(message);
    }
}