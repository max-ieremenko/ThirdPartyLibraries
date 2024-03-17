using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Validate.Internal;

internal interface IPackageValidator
{
    Task<ValidationResult> ValidateReferenceAsync(IPackageReference reference, string appName, CancellationToken token);

    Task<ValidationResult> ValidateLibraryAsync(LibraryId id, string appName, CancellationToken token);
}