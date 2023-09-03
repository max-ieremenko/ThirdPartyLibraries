using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal sealed class LicenseByUrlResolver : ILicenseByUrlResolver
{
    private readonly ILicenseByUrlLoader[] _loaders;

    public LicenseByUrlResolver(IEnumerable<ILicenseByUrlLoader> loaders)
    {
        _loaders = loaders.ToArray();
    }

    public Task<LicenseSpec?> TryResolveAsync(string url, CancellationToken token)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var location))
        {
            return Task.FromResult((LicenseSpec?)null);
        }

        return TryDownloadAsync(location, token);
    }

    private async Task<LicenseSpec?> TryDownloadAsync(Uri url, CancellationToken token)
    {
        LicenseSpec? result = null;

        for (var i = 0; i < _loaders.Length; i++)
        {
            var spec = await _loaders[i].TryDownloadAsync(url, token).ConfigureAwait(false);
            if (spec != null)
            {
                result = LicenseSpecComparer.GetTheBest(result, spec);
            }
        }

        return result;
    }
}