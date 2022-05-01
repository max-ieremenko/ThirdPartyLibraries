using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands
{
    internal sealed class GenerateCommandState
    {
        private readonly IDictionary<string, LicenseIndexJson> _indexByCode;
        private readonly IDictionary<string, ThirdPartyNoticesLicenseContext> _licenseByCode;

        public GenerateCommandState(IPackageRepository repository, string to, ILogger logger)
        {
            Repository = repository;
            To = to;
            Logger = logger;

            _indexByCode = new Dictionary<string, LicenseIndexJson>(StringComparer.OrdinalIgnoreCase);
            _licenseByCode = new Dictionary<string, ThirdPartyNoticesLicenseContext>(StringComparer.OrdinalIgnoreCase);
        }

        public IPackageRepository Repository { get; }

        public string To { get; }

        public ILogger Logger { get; }

        public IEnumerable<ThirdPartyNoticesLicenseContext> Licenses => _licenseByCode.Values;

        public async Task<ThirdPartyNoticesLicenseContext> GetLicensesAsync(string licenseExpression, CancellationToken token)
        {
            if (_licenseByCode.TryGetValue(licenseExpression, out var result))
            {
                return result;
            }

            var codes = LicenseExpression.GetCodes(licenseExpression);
            result = new ThirdPartyNoticesLicenseContext { FullName = licenseExpression };

            foreach (var code in codes)
            {
                var index = await LoadLicenseIndexAsync(code, token).ConfigureAwait(false);
                if (index != null)
                {
                    result.HRefs.Add(index.HRef);
                    if (!index.FileName.IsNullOrEmpty())
                    {
                        result.FileNames.Add(index.FileName);
                    }

                    if (codes.Count == 1)
                    {
                        result.FullName = index.FullName;
                    }
                }
            }

            _licenseByCode.Add(licenseExpression, result);
            return result;
        }

        private async Task<LicenseIndexJson> LoadLicenseIndexAsync(string code, CancellationToken token)
        {
            if (_indexByCode.TryGetValue(code, out var index))
            {
                return index;
            }

            index = await Repository.Storage.ReadLicenseIndexJsonAsync(code, token).ConfigureAwait(false);
            if (index == null)
            {
                Logger.Info("License {0} not found in the repository.".FormatWith(code));
                _indexByCode.Add(code, null);
                return null;
            }

            var result = new LicenseIndexJson
            {
                FullName = index.FullName.IsNullOrEmpty() ? index.Code : index.FullName,
                HRef = index.HRef
            };

            _indexByCode.Add(code, result);

            if (!index.FileName.IsNullOrEmpty())
            {
                Directory.CreateDirectory(Path.Combine(To, "Licenses"));
                result.FileName = "Licenses/{0}-{1}".FormatWith(index.Code, index.FileName);

                using (var content = await Repository.Storage.OpenLicenseFileReadAsync(code, index.FileName, token).ConfigureAwait(false))
                using (var dest = new FileStream(Path.Combine(To, result.FileName), FileMode.Create, FileAccess.ReadWrite))
                {
                    await content.CopyToAsync(dest, token).ConfigureAwait(false);
                }
            }

            if (!index.Dependencies.IsNullOrEmpty())
            {
                foreach (var dependency in index.Dependencies)
                {
                    await LoadLicenseIndexAsync(dependency, token).ConfigureAwait(false);
                }
            }

            return result;
        }
    }
}
