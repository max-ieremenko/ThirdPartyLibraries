using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
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

        public async Task<(IList<RootReadMeLicenseContext> Licenses, string MarkdownExpression)> GetLicensesAsync(string licenseExpression, CancellationToken token)
        {
            if (licenseExpression.IsNullOrEmpty())
            {
                return (Array.Empty<RootReadMeLicenseContext>(), null);
            }

            var codes = LicenseExpression.GetCodes(licenseExpression);

            var licenses = new RootReadMeLicenseContext[codes.Count];
            for (var i = 0; i < licenses.Length; i++)
            {
                var code = codes[i];
                if (!_licenseByCode.TryGetValue(code, out var license))
                {
                    var repositoryLicense = await Repository.LoadOrCreateLicenseAsync(code, token);
                    license = new RootReadMeLicenseContext
                    {
                        Code = repositoryLicense.Code,
                        RequiresApproval = repositoryLicense.RequiresApproval,
                        RequiresThirdPartyNotices = repositoryLicense.RequiresThirdPartyNotices,
                        LocalHRef = Repository.Storage.GetLicenseLocalHRef(repositoryLicense.Code)
                    };

                    _licenseByCode.Add(code, license);
                }

                licenses[i] = license;
            }

            var markdownExpression = LicenseExpression.ReplaceCodes(licenseExpression, code => "[{0}]({1})".FormatWith(code, _licenseByCode[code].LocalHRef));

            return (licenses, markdownExpression);
        }
    }
}
