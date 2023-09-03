using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal sealed class CustomPackageUpdater : ICustomPackageUpdater
{
    private readonly IStorage _storage;
    private readonly IStorageLicenseUpdater _storageLicense;

    public CustomPackageUpdater(IStorage storage, IStorageLicenseUpdater storageLicense)
    {
        _storage = storage;
        _storageLicense = storageLicense;
    }

    public async Task<List<LibraryId>> GetAllCustomLibrariesAsync(CancellationToken token)
    {
        var result = await _storage.GetAllLibrariesAsync(token).ConfigureAwait(false);

        result.RemoveAll(i => !i.IsCustomSource());

        return result;
    }

    public async Task UpdateAsync(LibraryId library, CancellationToken token)
    {
        if (!library.IsCustomSource())
        {
            throw new ArgumentOutOfRangeException(nameof(library));
        }

        var index = await _storage.ReadLibraryIndexJsonAsync<CustomLibraryIndexJson>(library, token).ConfigureAwait(false);
        if (index == null)
        {
            return;
        }

        await _storage.CreateDefaultThirdPartyNoticesFileAsync(library, token).ConfigureAwait(false);

        var codes = LicenseCode.FromText(index.LicenseCode).Codes;
        for (var i = 0; i < codes.Length; i++)
        {
            await _storageLicense.LoadOrCreateAsync(codes[i], token).ConfigureAwait(false);
        }
    }
}