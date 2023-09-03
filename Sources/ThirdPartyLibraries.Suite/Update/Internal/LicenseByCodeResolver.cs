using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal sealed class LicenseByCodeResolver : ILicenseByCodeResolver
{
    private readonly ILicenseByCodeLoader[] _loaders;

    public LicenseByCodeResolver(IEnumerable<ILicenseByCodeLoader> loaders)
    {
        _loaders = loaders.ToArray();
    }

    public async Task<LicenseSpec?> TryResolveAsync(string code, CancellationToken token)
    {
        LicenseSpec? result = null;

        for (var i = 0; i < _loaders.Length; i++)
        {
            var spec = await _loaders[i].TryDownloadAsync(code, token).ConfigureAwait(false);
            if (spec != null)
            {
                result = LicenseSpecComparer.GetTheBest(result, spec);
            }
        }

        return result;
    }
}