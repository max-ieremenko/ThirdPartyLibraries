using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal sealed class LicenseByContentResolver : ILicenseByContentResolver
{
    private readonly IStorage _storage;
    private readonly ILicenseHashBuilder _hashBuilder;
    private readonly Dictionary<string, StorageLicenseFile?> _storageLicenseByCode;

    public LicenseByContentResolver(IStorage storage, ILicenseHashBuilder hashBuilder)
    {
        _storage = storage;
        _hashBuilder = hashBuilder;
        _storageLicenseByCode = new Dictionary<string, StorageLicenseFile?>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IdenticalLicenseFile?> TryResolveAsync(LibraryId library, CancellationToken token)
    {
        var (_, hash) = await _hashBuilder.GetHashAsync(library, PackageSpecLicense.SubjectPackage, token).ConfigureAwait(false);

        // no license file
        if (!hash.HasValue)
        {
            return null;
        }

        var result = await LookAtOtherVersionsAsync(library, hash.Value, token).ConfigureAwait(false);
        if (result == null)
        {
            result = await LookAtLicensesAsync(hash.Value, token).ConfigureAwait(false);
        }

        return result;
    }

    private async Task<IdenticalLicenseFile?> LookAtOtherVersionsAsync(LibraryId library, ArrayHash hash, CancellationToken token)
    {
        var versions = await _storage.GetAllLibraryVersionsAsync(library.SourceCode, library.Name, token).ConfigureAwait(false);
        for (var i = 0; i < versions.Count; i++)
        {
            var other = versions[i];
            if (other.Version.Equals(library.Version, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var (_, otherHash) = await _hashBuilder.GetHashAsync(other, PackageSpecLicense.SubjectPackage, token).ConfigureAwait(false);
            if (!otherHash.HasValue || !otherHash.Value.Equals(hash))
            {
                continue;
            }

            var otherIndex = await _storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(other, token).ConfigureAwait(false);
            if (string.IsNullOrEmpty(otherIndex?.License.Code))
            {
                continue;
            }

            return new IdenticalLicenseFile(otherIndex.License.Code, $"The license file is identical to the file from version {other.Version}.");
        }

        return null;
    }

    private async Task<IdenticalLicenseFile?> LookAtLicensesAsync(ArrayHash hash, CancellationToken token)
    {
        var codes = await _storage.GetAllLicenseCodesAsync(token).ConfigureAwait(false);

        for (var i = 0; i < codes.Count; i++)
        {
            var file = await GetStorageLicenseAsync(codes[i], token).ConfigureAwait(false);
            if (file != null && file.Hash.Equals(hash))
            {
                return file.File;
            }
        }

        return null;
    }

    private async Task<StorageLicenseFile?> GetStorageLicenseAsync(string code, CancellationToken token)
    {
        if (_storageLicenseByCode.TryGetValue(code, out var result))
        {
            return result;
        }

        var index = await _storage.ReadLicenseIndexJsonAsync(code, token).ConfigureAwait(false);
        if (index == null)
        {
            return null;
        }

        ArrayHash? hash = null;
        if (!string.IsNullOrEmpty(index.FileName))
        {
            hash = await _hashBuilder.GetHashAsync(index.Code, index.FileName, token).ConfigureAwait(false);
        }

        result = null;
        if (hash.HasValue)
        {
            result = new StorageLicenseFile(
                new IdenticalLicenseFile(index.Code, $"The license file is identical to the {index.Code} license file."),
                hash.Value);
        }

        _storageLicenseByCode.Add(code, result);
        return result;
    }

    private sealed class StorageLicenseFile
    {
        public StorageLicenseFile(IdenticalLicenseFile file, ArrayHash hash)
        {
            File = file;
            Hash = hash;
        }

        public IdenticalLicenseFile File { get; }
        
        public ArrayHash Hash { get; }
    }
}