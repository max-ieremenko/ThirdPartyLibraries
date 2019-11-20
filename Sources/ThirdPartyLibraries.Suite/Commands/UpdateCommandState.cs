using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands
{
    internal sealed class UpdateCommandState
    {
        private readonly HashSet<LibraryId> _processed;
        private readonly IDictionary<string, RepositoryLicense> _licenseByCode;

        public UpdateCommandState(IPackageRepository repository)
        {
            Repository = repository;

            _processed = new HashSet<LibraryId>();
            _licenseByCode = new Dictionary<string, RepositoryLicense>(StringComparer.OrdinalIgnoreCase);
        }
        
        public IPackageRepository Repository { get; }

        public Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token)
        {
            _processed.Add(id);

            return Repository.LoadPackageAsync(id, token);
        }

        public Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token)
        {
            _processed.Add(reference.Id);

            return Repository.UpdatePackageAsync(reference, package, appName, token);
        }

        public async ValueTask<bool> LicenseRequiresApprovalAsync(string licenseExpression, CancellationToken token)
        {
            var codes = LicenseExpression.GetCodes(licenseExpression);

            var result = false;
            foreach (var code in codes)
            {
                var license = await LoadLicenseAsync(code, token);
                if (license.RequiresApproval)
                {
                    result = true;
                }
            }

            return result;
        }

        public async Task<IList<LibraryId>> GetIdsToRemoveAsync(CancellationToken token)
        {
            var allIds = await Repository.Storage.GetAllLibrariesAsync(token);

            return allIds.Where(i => !_processed.Contains(i)).ToList();
        }

        private async Task<RepositoryLicense> LoadLicenseAsync(string code, CancellationToken token)
        {
            if (_licenseByCode.TryGetValue(code, out var license))
            {
                return license;
            }

            license = await Repository.LoadOrCreateLicenseAsync(code, token);
            _licenseByCode.Add(license.Code, license);

            if (!license.Dependencies.IsNullOrEmpty())
            {
                foreach (var dependency in license.Dependencies)
                {
                    await LoadLicenseAsync(dependency, token);
                }
            }

            return license;
        }
    }
}
