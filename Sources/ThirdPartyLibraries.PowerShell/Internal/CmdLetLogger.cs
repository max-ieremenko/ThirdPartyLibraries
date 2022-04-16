using System;
using System.Management.Automation;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.PowerShell.Internal
{
    internal class CmdLetLogger : LoggerBase
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

        protected override void OnInfo(string message)
        {
            _cmdlet.WriteInformation(new InformationRecord(message, null));
        }
    }
}
