using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Validate.Internal;

internal sealed class PackageValidator : IPackageValidator
{
    private readonly IStorage _storage;
    private readonly Dictionary<string, LicenseIndexJson?> _licenseByCode;

    public PackageValidator(IStorage storage)
    {
        _storage = storage;
        _licenseByCode = new Dictionary<string, LicenseIndexJson?>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ValidationResult> ValidateReferenceAsync(IPackageReference reference, string appName, CancellationToken token)
    {
        var index = await _storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(reference.Id, token).ConfigureAwait(false);
        if (index == null)
        {
            return ValidationResult.IndexNotFound;
        }

        if (!IsAssignedToApp(index, appName))
        {
            return ValidationResult.NotAssignedToIndex;
        }

        var licenseCode = LicenseCode.FromText(index.License.Code);
        if (licenseCode.IsEmpty)
        {
            return ValidationResult.NoLicenseCode;
        }

        var requiresApproval = false;
        var requiresThirdPartyNotices = false;
        for (var i = 0; i < licenseCode.Codes.Length; i++)
        {
            var licenseIndex = await GetLicenseIndexAsync(licenseCode.Codes[i], token).ConfigureAwait(false);
            if (licenseIndex == null)
            {
                return ValidationResult.LicenseNotFound;
            }

            requiresApproval = requiresApproval || licenseIndex.RequiresApproval;
            requiresThirdPartyNotices = requiresThirdPartyNotices || licenseIndex.RequiresThirdPartyNotices;
        }
        
        var licenseResult = ValidateLicenseStatus(index.License.Status, requiresApproval);

        var thirdPartyNoticesResult = ValidationResult.Success;
        if (requiresThirdPartyNotices)
        {
            var thirdPartyNotices = await _storage.ReadThirdPartyNoticesFileAsync(reference.Id, token).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(thirdPartyNotices))
            {
                thirdPartyNoticesResult = ValidationResult.NoThirdPartyNotices;
            }
        }

        return licenseResult | thirdPartyNoticesResult;
    }

    public async Task<ValidationResult> ValidateLibraryAsync(LibraryId id, string appName, CancellationToken token)
    {
        var index = await _storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(id, token).ConfigureAwait(false);
        if (index == null)
        {
            return ValidationResult.IndexNotFound;
        }

        return IsAssignedToApp(index, appName) ? ValidationResult.ReferenceNotFound : ValidationResult.Success;
    }

    private static bool IsAssignedToApp(LibraryIndexJson index, string appName)
    {
        return index.UsedBy.Exists(i => appName.Equals(i.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static ValidationResult ValidateLicenseStatus(string? status, bool requiresApproval)
    {
        if (!PackageLicenseApprovalStatus.IsDefined(status))
        {
            return ValidationResult.LicenseNotApproved;
        }

        if (PackageLicenseApprovalStatus.IsApproved(status))
        {
            return ValidationResult.Success;
        }

        if (!PackageLicenseApprovalStatus.IsAutomaticallyApproved(status))
        {
            return ValidationResult.LicenseNotApproved;
        }

        return requiresApproval ? ValidationResult.LicenseNotApproved : ValidationResult.Success;
    }

    private async Task<LicenseIndexJson?> GetLicenseIndexAsync(string code, CancellationToken token)
    {
        if (_licenseByCode.TryGetValue(code, out var result))
        {
            return result;
        }

        result = await _storage.ReadLicenseIndexJsonAsync(code, token).ConfigureAwait(false);
        _licenseByCode.Add(code, result);

        return result;
    }
}