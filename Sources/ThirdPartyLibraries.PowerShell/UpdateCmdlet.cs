using System.Management.Automation;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.PowerShell.Internal;

namespace ThirdPartyLibraries.PowerShell;

[Cmdlet(VerbsData.Update, "ThirdPartyLibrariesRepository")]
public sealed class UpdateCmdlet : CommandCmdlet
{
    [Parameter(Mandatory = true, Position = 1, HelpMessage = "a name of the current application")]
    public string AppName { get; set; } = null!;

    [Parameter(Mandatory = true, Position = 2, HelpMessage = "a path(s) to a folder with solution/projects or to a project file. Folder will be analized recursively")]
    public string[] Source { get; set; } = null!;

    [Parameter(Mandatory = true, Position = 3, HelpMessage = "a path to a repository folder")]
    public string Repository { get; set; } = null!;

    [Parameter(Position = 4, HelpMessage = "optional personal access token for github.com web api")]
    public string? GithubPersonalAccessToken { get; set; }

    protected override string CreateCommandLine(IList<(string Name, string Value)> options)
    {
        options.Add((CommandOptions.OptionAppName, AppName));
        options.Add((CommandOptions.OptionRepository, this.RootPath(Repository)));

        for (var i = 0; i < Source.Length; i++)
        {
            options.Add((CommandOptions.OptionSource, this.RootPath(Source[i])));
        }

        if (!string.IsNullOrEmpty(GithubPersonalAccessToken))
        {
            options.Add((CommandOptions.OptionGitHubToken, GithubPersonalAccessToken));
        }

        return CommandOptions.CommandUpdate;
    }
}