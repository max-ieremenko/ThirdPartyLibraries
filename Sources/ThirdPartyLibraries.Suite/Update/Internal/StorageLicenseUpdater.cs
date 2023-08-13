using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal sealed class StorageLicenseUpdater : IStorageLicenseUpdater
{
    private readonly IStorage _storage;
    private readonly ILicenseByCodeResolver _licenseResolver;
    private readonly Dictionary<string, LicenseIndexJson> _storageLicenseByCode;

    public StorageLicenseUpdater(IStorage storage, ILicenseByCodeResolver licenseResolver)
    {
        _storage = storage;
        _licenseResolver = licenseResolver;
        _storageLicenseByCode = new Dictionary<string, LicenseIndexJson>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<LicenseIndexJson?> LoadOrCreateAsync(string licenseCode, CancellationToken token)
    {
        if (NoneLicenseCode.IsNone(licenseCode))
        {
            return null;
        }

        if (_storageLicenseByCode.TryGetValue(licenseCode, out var index))
        {
            return index;
        }

        index = await _storage.ReadLicenseIndexJsonAsync(licenseCode, token).ConfigureAwait(false);
        if (index != null)
        {
            _storageLicenseByCode.Add(licenseCode, index);
            return index;
        }

        var spec = await _licenseResolver.TryResolveAsync(licenseCode, token).ConfigureAwait(false);

        var fileName = "license.txt";
        if (spec != null)
        {
            fileName = string.IsNullOrEmpty(spec.FileName) ? "license" + spec.FileExtension : spec.FileName;
        }

        index = new LicenseIndexJson
        {
            Code = spec?.Code ?? licenseCode,
            FullName = spec?.FullName,
            HRef = spec?.HRef,
            FileName = fileName,
            RequiresApproval = true,
            RequiresThirdPartyNotices = false
        };

        await _storage.CreateLicenseIndexJsonAsync(index, token).ConfigureAwait(false);
        _storageLicenseByCode.Add(licenseCode, index);

        var fileContent = spec?.FileContent ?? Array.Empty<byte>();
        await _storage.CreateLicenseFileAsync(index.Code, index.FileName, fileContent, token).ConfigureAwait(false);

        return index;
    }
}