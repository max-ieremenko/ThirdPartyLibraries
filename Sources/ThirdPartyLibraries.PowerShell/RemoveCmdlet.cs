using System.Management.Automation;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.PowerShell.Internal;

namespace ThirdPartyLibraries.PowerShell;

[Cmdlet(VerbsCommon.Remove, "AppFromThirdPartyLibrariesRepository")]
public sealed class RemoveCmdlet : CommandCmdlet
{
    [Parameter(Mandatory = true, Position = 1, HelpMessage = "a name of the current application")]
    public string[] AppName { get; set; } = null!;

    [Parameter(Mandatory = true, Position = 2, HelpMessage = "a path to a repository folder")]
    public string Repository { get; set; } = null!;

    protected override string CreateCommandLine(IList<(string Name, string Value)> options)
    {
        for (var i = 0; i < AppName.Length; i++)
        {
            options.Add((CommandOptions.OptionAppName, AppName[i]));
        }

        options.Add((CommandOptions.OptionRepository, this.RootPath(Repository)));

        return CommandOptions.CommandRemove;
    }
}