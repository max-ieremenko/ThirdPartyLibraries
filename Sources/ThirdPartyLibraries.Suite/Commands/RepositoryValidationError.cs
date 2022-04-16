using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Suite.Commands;

internal sealed class RepositoryValidationError
{
    public RepositoryValidationError(string issue, params LibraryId[] libraries)
    {
        Issue = issue;
        Libraries = libraries;
    }

    public string Issue { get; }
    
    public LibraryId[] Libraries { get; }
}