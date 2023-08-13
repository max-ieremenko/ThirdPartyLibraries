namespace ThirdPartyLibraries.Domain;

public interface IPackageSpec
{
    string GetName();

    string GetVersion();

    string? GetDescription();

    string? GetRepositoryUrl();

    string? GetCopyright();
    
    string? GetAuthor();
}