using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal sealed class PackageLicenseUpdater : IPackageLicenseUpdater
{
    private readonly IStorage _storage;
    private readonly ILicenseByUrlResolver _licenseByUrlResolver;
    private readonly ILicenseByContentResolver _licenseByContentResolver;
    private readonly IStorageLicenseUpdater _storageLicense;

    public PackageLicenseUpdater(
        IStorage storage,
        ILicenseByUrlResolver licenseByUrlResolver,
        ILicenseByContentResolver licenseByContentResolver,
        IStorageLicenseUpdater storageLicense)
    {
        _storage = storage;
        _licenseByUrlResolver = licenseByUrlResolver;
        _licenseByContentResolver = licenseByContentResolver;
        _storageLicense = storageLicense;
    }

    public async Task<bool> UpdateAsync(LibraryId library, CancellationToken token)
    {
        var index = await _storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(library, token).ConfigureAwait(false);
        if (index == null)
        {
            throw new InvalidOperationException($"The index must already be created by {nameof(PackageContentUpdater)}.");
        }

        await ResolveLicenseCodesAsync(library, index, token).ConfigureAwait(false);
        await EnsureLicensesExistAsync(index, token).ConfigureAwait(false);

        TrySolveRootLicenseCode(index);
        await UpdateConclusionAsync(index.License, token).ConfigureAwait(false);

        return await SaveLibraryIndexJsonAsync(library, index, token).ConfigureAwait(false);
    }

    private void TrySolveRootLicenseCode(LibraryIndexJson index)
    {
        if (!string.IsNullOrEmpty(index.License.Code))
        {
            return;
        }

        for (var i = 0; i < index.Licenses.Count; i++)
        {
            var license = index.Licenses[i];

            // copy from the package code
            if (PackageSpecLicense.SubjectPackage.Equals(license.Subject))
            {
                index.License.Code = license.Code;
                return;
            }
        }
    }

    private async Task UpdateConclusionAsync(LicenseConclusion conclusion, CancellationToken token)
    {
        if (string.IsNullOrEmpty(conclusion.Code))
        {
            conclusion.Status = PackageLicenseApprovalStatus.CodeHasToBeApproved;
            return;
        }

        if (!PackageLicenseApprovalStatus.IsDefined(conclusion.Status))
        {
            conclusion.Status = PackageLicenseApprovalStatus.CodeHasToBeApproved;
        }

        if (PackageLicenseApprovalStatus.IsApproved(conclusion.Status) || PackageLicenseApprovalStatus.IsAutomaticallyApproved(conclusion.Status))
        {
            return;
        }

        var codes = LicenseCode.FromText(conclusion.Code).Codes;
        var requiresApproval = false;
        for (var i = 0; i < codes.Length; i++)
        {
            var index = await _storageLicense.LoadOrCreateAsync(codes[i], token).ConfigureAwait(false);
            if (index == null || index.RequiresApproval)
            {
                requiresApproval = true;
                break;
            }
        }

        conclusion.Status = requiresApproval ? PackageLicenseApprovalStatus.CodeHasToBeApproved : PackageLicenseApprovalStatus.CodeAutomaticallyApproved;
    }

    private async Task EnsureLicensesExistAsync(LibraryIndexJson index, CancellationToken token)
    {
        var codes = LicenseCode.FromText(index.License.Code).Codes;
        for (var i = 0; i < codes.Length; i++)
        {
            await _storageLicense.LoadOrCreateAsync(codes[i], token).ConfigureAwait(false);
        }

        for (var i = 0; i < index.Licenses.Count; i++)
        {
            codes = LicenseCode.FromText(index.Licenses[i].Code).Codes;
            for (var j = 0; j < codes.Length; j++)
            {
                await _storageLicense.LoadOrCreateAsync(codes[j], token).ConfigureAwait(false);
            }
        }
    }

    private async Task ResolveLicenseCodesAsync(LibraryId id, LibraryIndexJson index, CancellationToken token)
    {
        // conclusion is done
        if (!string.IsNullOrEmpty(index.License.Code))
        {
            return;
        }

        for (var i = 0; i < index.Licenses.Count; i++)
        {
            var license = index.Licenses[i];

            // conclusion is not done
            if (string.IsNullOrEmpty(license.Code))
            {
                await TryResolveByHRefAsync(id, license, token).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(license.Code) && PackageSpecLicense.SubjectPackage.Equals(license.Subject))
            {
                await TryResolveByContentAsync(id, license, token).ConfigureAwait(false);
            }
        }
    }

    private async Task TryResolveByHRefAsync(LibraryId id, LibraryLicense license, CancellationToken token)
    {
        if (string.IsNullOrEmpty(license.HRef))
        {
            return;
        }

        var spec = await _licenseByUrlResolver.TryResolveAsync(license.HRef, token).ConfigureAwait(false);
        if (spec == null)
        {
            license.Description = null;
            return;
        }

        if (NoneLicenseCode.IsNone(spec.Code))
        {
            license.Code = null;
            license.Description = $"License code {spec.Code}";
        }
        else
        {
            license.Code = spec.Code;
            license.Description = null;
        }

        if (spec.Source != LicenseSpecSource.UserDefined || spec.FileContent == null)
        {
            return;
        }

        var fileName = string.IsNullOrEmpty(spec.FileName) ? "license" + spec.FileExtension : spec.FileName;
        fileName = PackageStorageLicenseFile.GetName(license.Subject, fileName);

        var exists = await _storage.LibraryFileExistsAsync(id, fileName, token).ConfigureAwait(false);
        if (exists)
        {
            return;
        }

        await _storage.WriteLibraryFileAsync(id, fileName, spec.FileContent, token).ConfigureAwait(false);
    }

    private async Task TryResolveByContentAsync(LibraryId id, LibraryLicense license, CancellationToken token)
    {
        var file = await _licenseByContentResolver.TryResolveAsync(id, token).ConfigureAwait(false);
        if (file != null)
        {
            license.Code = file.LicenseCode;
            license.Description = file.Description;
        }
    }

    private async Task<bool> SaveLibraryIndexJsonAsync(LibraryId id, LibraryIndexJson index, CancellationToken token)
    {
        var original = await _storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(id, token).ConfigureAwait(false);

        if (LibraryIndexJsonChangeTracker.IsChanged(original, index))
        {
            LibraryIndexJsonChangeTracker.SortValues(index);
            await _storage.WriteLibraryIndexJsonAsync(id, index, token).ConfigureAwait(false);

            return true;
        }

        return false;
    }
}