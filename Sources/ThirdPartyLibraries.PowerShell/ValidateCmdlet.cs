using System.Management.Automation;
using System.Threading;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.PowerShell.Internal;

namespace ThirdPartyLibraries.PowerShell;

[Cmdlet(VerbsDiagnostic.Test, "ThirdPartyLibrariesRepository")]
[Alias("Validate-ThirdPartyLibrariesRepository")]
public sealed class ValidateCmdlet : CommandCmdlet
{
    [Parameter(Mandatory = true, Position = 1, HelpMessage = "a name of the current application")]
    public string AppName { get; set; }

    [Parameter(Mandatory = true, Position = 2, HelpMessage = "a path(s) to a folder with solution/projects or to a project file. Folder will be analized recursively")]
    public string[] Source { get; set; }

    [Parameter(Mandatory = true, Position = 3, HelpMessage = "a path to a repository folder")]
    public string Repository { get; set; }

    protected override void ProcessRecord(CancellationToken token)
    {
        var commandLine = new CommandLine
        {
            Command = CommandOptions.CommandValidate,
            Options =
            {
                new CommandOption(CommandOptions.OptionAppName, AppName),
                new CommandOption(CommandOptions.OptionRepository, this.RootPath(Repository))
            }
        };

        for (var i = 0; i < Source.Length; i++)
        {
            commandLine.Options.Add(new CommandOption(CommandOptions.OptionSource, this.RootPath(Source[i])));
        }

        ThirdPartyLibrariesProgram.Run(commandLine, this, token);
    }
}