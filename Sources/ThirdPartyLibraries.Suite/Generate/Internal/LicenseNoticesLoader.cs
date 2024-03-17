using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal sealed class LicenseNoticesLoader : ILicenseNoticesLoader
{
    private readonly IStorage _storage;
    private readonly ILicenseHashBuilder _hashBuilder;

    public LicenseNoticesLoader(IStorage storage, ILicenseHashBuilder hashBuilder)
    {
        _storage = storage;
        _hashBuilder = hashBuilder;
    }

    public async Task<LicenseNotices> LoadAsync(LicenseCode code, CancellationToken token)
    {
        if (code.IsEmpty)
        {
            throw new ArgumentNullException(nameof(code));
        }

        var result = new LicenseNotices
        {
            Code = code.Text!,
            FullName = code.Text!
        };

        for (var i = 0; i < code.Codes.Length; i++)
        {
            var index = await _storage.ReadLicenseIndexJsonAsync(code.Codes[i], token).ConfigureAwait(false);
            if (index == null)
            {
                continue;
            }

            if (Uri.TryCreate(index.HRef, UriKind.Absolute, out var href))
            {
                result.HRefs.Add(href);
            }

            if (!string.IsNullOrEmpty(index.FileName))
            {
                var file = await TryLoadFileAsync(index.Code, index.FileName, token).ConfigureAwait(false);
                if (file != null)
                {
                    result.Files.Add(file);
                }
            }
            
            if (code.Codes.Length == 1 && !string.IsNullOrEmpty(index.FullName))
            {
                result.FullName = index.FullName;
            }
        }

        return result;
    }

    private async Task<LicenseFile?> TryLoadFileAsync(string code, string fileName, CancellationToken token)
    {
        var hash = await _hashBuilder.GetHashAsync(code, fileName, token).ConfigureAwait(false);

        if (!hash.HasValue)
        {
            return null;
        }

        return new LicenseFile(code, fileName, hash.Value);
    }
}