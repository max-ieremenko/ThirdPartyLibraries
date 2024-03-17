using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Suite.Shared.Internal;

internal sealed class LicenseHashBuilder : ILicenseHashBuilder
{
    private readonly IStorage _storage;

    public LicenseHashBuilder(IStorage storage)
    {
        _storage = storage;
    }

    public async Task<ArrayHash?> GetHashAsync(string licenseCode, string fileName, CancellationToken token)
    {
        using var stream = await _storage.OpenLicenseFileReadAsync(licenseCode, fileName, token).ConfigureAwait(false);
        
        return ArrayHashBuilder.FromStream(stream);
    }

    public async Task<(string? FileName, ArrayHash? Hash)> GetHashAsync(LibraryId library, string licenseSubject, CancellationToken token)
    {
        var fileNames = await _storage
            .FindLibraryFilesAsync(library, PackageStorageLicenseFile.GetMask(licenseSubject), token)
            .ConfigureAwait(false);

        if (fileNames.Length == 0)
        {
            return (null, null);
        }

        using var stream = await _storage.OpenLibraryFileReadAsync(library, fileNames[0], token).ConfigureAwait(false);
        var hash = ArrayHashBuilder.FromStream(stream);

        return (fileNames[0], hash);
    }
}