using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Validate.Internal;

internal sealed class RepositoryValidationError
{
    public RepositoryValidationError(ValidationResult issue, string appName, params LibraryId[] libraries)
    {
        Issue = issue;
        AppName = appName;
        Libraries = libraries;
    }

    public ValidationResult Issue { get; }

    public string AppName { get; }

    public LibraryId[] Libraries { get; }
}