using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands
{
    internal sealed class RefreshCommandState
    {
        private readonly IDictionary<string, RootReadMeLicenseContext> _licenseByCode;

        public RefreshCommandState(IPackageRepository repository)
        {
            Repository = repository;

            _licenseByCode = new Dictionary<string, RootReadMeLicenseContext>(StringComparer.OrdinalIgnoreCase);
        }

        public IPackageRepository Repository { get; }

        public IEnumerable<RootReadMeLicenseContext> Licenses => _licenseByCode.Values;

        public async Task<IList<RootReadMeLicenseContext>> GetLicensesAsync(string licenseExpression, CancellationToken token)
        {
            var codes = LicenseExpression.GetCodes(licenseExpression);

            var result = new RootReadMeLicenseContext[codes.Count];
            for (var i = 0; i < result.Length; i++)
            {
                var code = codes[i];
                if (!_licenseByCode.TryGetValue(code, out var license))
                {
                    var repositoryLicense = await Repository.LoadLicenseAsync(code, token);
                    license = new RootReadMeLicenseContext
                    {
                        Code = repositoryLicense.Code,
                        RequiresApproval = repositoryLicense.RequiresApproval ? "yes" : "no",
                        LocalHRef = Repository.Storage.GetLicenseLocalHRef(repositoryLicense.Code, RelativeTo.Root)
                    };

                    _licenseByCode.Add(code, license);
                }

                result[i] = license;
            }

            return result;
        }
    }
}
