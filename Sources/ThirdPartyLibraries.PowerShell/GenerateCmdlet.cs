﻿using System.Management.Automation;
using System.Threading;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.PowerShell.Internal;

namespace ThirdPartyLibraries.PowerShell;

[Cmdlet(VerbsData.Publish, "ThirdPartyNotices")]
[Alias("Generate-ThirdPartyNotices")]
public sealed class GenerateCmdlet : CommandCmdlet
{
    [Parameter(Mandatory = true, Position = 1, HelpMessage = "a name of the current application")]
    public string AppName { get; set; }

    [Parameter(Mandatory = true, Position = 2, HelpMessage = "a path to a repository folder")]
    public string Repository { get; set; }

    [Parameter(Mandatory = true, Position = 3, HelpMessage = "a path to an output folder")]
    public string To { get; set; }

    protected override void ProcessRecord(CancellationToken token)
    {
        var commandLine = new CommandLine
        {
            Command = CommandOptions.CommandGenerate,
            Options =
            {
                new CommandOption(CommandOptions.OptionAppName, AppName),
                new CommandOption(CommandOptions.OptionRepository, this.RootPath(Repository)),
                new CommandOption(CommandOptions.OptionTo, this.RootPath(To))
            }
        };

        ThirdPartyLibrariesProgram.Run(commandLine, this, token);
    }
}