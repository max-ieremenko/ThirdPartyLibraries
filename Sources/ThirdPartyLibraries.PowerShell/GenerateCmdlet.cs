using System.Management.Automation;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.PowerShell.Internal;

namespace ThirdPartyLibraries.PowerShell;

[Cmdlet(VerbsData.Publish, "ThirdPartyNotices")]
[Alias("Generate-ThirdPartyNotices")]
public sealed class GenerateCmdlet : CommandCmdlet
{
    [Parameter(Mandatory = true, Position = 1, HelpMessage = "a name of the current application")]
    public string[] AppName { get; set; } = null!;

    [Parameter(Mandatory = true, Position = 2, HelpMessage = "a path to a repository folder")]
    public string Repository { get; set; } = null!;

    [Parameter(Mandatory = true, Position = 3, HelpMessage = "a path to an output folder")]
    public string To { get; set; } = null!;

    [Parameter(Position = 4, HelpMessage = "a title of third party notices, default is appName[0]")]
    public string? Title { get; set; }

    [Parameter(Position = 5, HelpMessage = "output file name, default is ThirdPartyNotices.txt")]
    public string? ToFileName { get; set; }

    [Parameter(Position = 6, HelpMessage = "a path to a DotLiquid template file, default is configuration/third-party-notices-template.txt in the repository folder")]
    public string? Template { get; set; }

    protected override string CreateCommandLine(IList<(string Name, string Value)> options)
    {
        options.Add((CommandOptions.OptionRepository, this.RootPath(Repository)));
        options.Add((CommandOptions.OptionTo, this.RootPath(To)));

        if (!string.IsNullOrEmpty(Title))
        {
            options.Add((CommandOptions.OptionTitle, Title));
        }

        for (var i = 0; i < AppName.Length; i++)
        {
            options.Add((CommandOptions.OptionAppName, AppName[i]));
        }

        if (!string.IsNullOrEmpty(ToFileName))
        {
            options.Add((CommandOptions.OptionToFileName, ToFileName));
        }

        if (!string.IsNullOrEmpty(Template))
        {
            options.Add((CommandOptions.OptionTemplate, this.RootPath(Template)));
        }

        return CommandOptions.CommandGenerate;
    }
}