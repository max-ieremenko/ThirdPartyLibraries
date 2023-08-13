using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Generic.Internal;

internal sealed class OpenSourceLicenseLoader : ILicenseByUrlLoader, ILicenseByCodeLoader
{
    private readonly SpdxOrgRepository _spdxOrg;
    private readonly OpenSourceOrgRepository _openSourceOrg;

    public OpenSourceLicenseLoader(SpdxOrgRepository spdxOrg, OpenSourceOrgRepository openSourceOrg)
    {
        _spdxOrg = spdxOrg;
        _openSourceOrg = openSourceOrg;
    }

    public async Task<LicenseSpec?> TryDownloadAsync(Uri url, CancellationToken token)
    {
        await _openSourceOrg.LoadIndexAsync(token).ConfigureAwait(false);

        if (_openSourceOrg.TryFindLicenseCodeByUrl(url, out var code))
        {
            return new LicenseSpec(LicenseSpecSource.Shared, code);
        }

        if (!_spdxOrg.TryParseLicenseCode(url, out code))
        {
            return null;
        }

        if (_openSourceOrg.TryFindLicenseCode(code, out var validCode))
        {
            return new LicenseSpec(LicenseSpecSource.Shared, validCode);
        }

        var result = await _spdxOrg.TryDownloadByCodeAsync(code, token).ConfigureAwait(false);
        return result;
    }

    public async Task<LicenseSpec?> TryDownloadAsync(string code, CancellationToken token)
    {
        var result = await _spdxOrg.TryDownloadByCodeAsync(code, token).ConfigureAwait(false);
        if (result != null)
        {
            return result;
        }

        await _openSourceOrg.LoadIndexAsync(token).ConfigureAwait(false);
        if (!_openSourceOrg.TryFindLicenseCode(code, out var validCode))
        {
            return null;
        }

        if (!validCode.Equals(code, StringComparison.Ordinal))
        {
            result = await _spdxOrg.TryDownloadByCodeAsync(validCode, token).ConfigureAwait(false);
            if (result != null)
            {
                return result;
            }
        }

        result = await _openSourceOrg.TryDownloadByCodeAsync(validCode, token).ConfigureAwait(false);
        return result;
    }
}