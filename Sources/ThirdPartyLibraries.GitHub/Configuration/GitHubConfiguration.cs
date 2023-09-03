namespace ThirdPartyLibraries.GitHub.Configuration;

public sealed class GitHubConfiguration
{
    public const string SectionName = "github.com";

    public string? PersonalAccessToken { get; set; }
}