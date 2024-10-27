namespace ThirdPartyLibraries.GitHub.Internal.Domain;

internal sealed class GitHubRepositoryLicense
{
    public string? Name { get; set; }

    public string? Encoding { get; set; }

    public string? Content { get; set; }

    public GitHubRepositoryLicenseContent? License { get; set; }
}