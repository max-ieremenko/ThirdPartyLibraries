using System;
using System.Collections.Concurrent;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal sealed class LicenseCache : ILicenseCache
    {
        private readonly ConcurrentDictionary<string, LicenseInfo> _byUrl = new ConcurrentDictionary<string, LicenseInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, LicenseInfo> _byCode = new ConcurrentDictionary<string, LicenseInfo>(StringComparer.OrdinalIgnoreCase);

        public bool TryGetByUrl(string url, out LicenseInfo info) => _byUrl.TryGetValue(url, out info);

        public void AddByUrl(string url, LicenseInfo info) => _byUrl.TryAdd(url, info);

        public bool TryGetByCode(string code, out LicenseInfo info) => _byCode.TryGetValue(code, out info);

        public void AddByCode(string code, LicenseInfo info) => _byCode.TryAdd(code, info);
    }
}
