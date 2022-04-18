using System;
using System.Management.Automation;

namespace ThirdPartyLibraries.PowerShell.Internal
{
    internal class CmdLetLogger
    {
        private readonly Cmdlet _cmdlet;

        public CmdLetLogger(Cmdlet cmdlet)
        {
            _cmdlet = cmdlet;
        }

        public void Error(Exception exception)
        {
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
    }
}
