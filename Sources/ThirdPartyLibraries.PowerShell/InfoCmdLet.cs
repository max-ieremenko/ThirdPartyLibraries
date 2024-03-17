using System.Collections;
using System.Management.Automation;
using System.Runtime.InteropServices;
using ThirdPartyLibraries.PowerShell.Internal;

namespace ThirdPartyLibraries.PowerShell;

[Cmdlet(VerbsCommon.Show, "ThirdPartyLibrariesInfo")]
public sealed class InfoCmdLet : PSCmdlet
{
    protected override void ProcessRecord()
    {
        var assembly = GetType().Assembly;
        var psVersionTable = (Hashtable)GetVariableValue("PSVersionTable");

        WriteObject(new
        {
            PSEdition = psVersionTable["PSEdition"],
            PSVersion = psVersionTable["PSVersion"],
            assembly.GetName().Version,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture,
            RuntimeInformation.ProcessArchitecture,
            Location = Path.GetDirectoryName(assembly.Location),
            WorkingDirectory = this.GetWorkingDirectory()
        });
    }
}