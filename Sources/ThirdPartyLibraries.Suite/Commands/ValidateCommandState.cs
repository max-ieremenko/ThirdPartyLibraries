using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands
{
    internal sealed class ValidateCommandState
    {
        private readonly HashSet<LibraryId> _appPackages;
        private readonly IDictionary<LibraryId, (string LicenseCode, PackageApprovalStatus Status, bool HasThirdPartyNotices)> _infoById;
        private readonly IDictionary<string, LicenseIndexJson> _licenseByCode;

        public ValidateCommandState(IPackageRepository repository, string appName)
        {
            Repository = repository;
            AppName = appName;

            _appPackages = new HashSet<LibraryId>();
            _infoById = new Dictionary<LibraryId, (string, PackageApprovalStatus, bool)>();
            _licenseByCode = new Dictionary<string, LicenseIndexJson>(StringComparer.OrdinalIgnoreCase);
        }

        public IPackageRepository Repository { get; }
        
        public string AppName { get; }

        public async Task InitializeAsync(CancellationToken token)
        {
            var libraries = await Repository.Storage.GetAllLibrariesAsync(token).ConfigureAwait(false);

            foreach (var id in libraries)
            {
                var package = await Repository.LoadPackageAsync(id, token).ConfigureAwait(false);
                await LoadLicenseAsync(package.LicenseCode, token).ConfigureAwait(false);

                _infoById.Add(id, (package.LicenseCode, package.ApprovalStatus, !package.ThirdPartyNotices.IsNullOrEmpty()));

                if (!package.SourceCode.Equals(PackageSources.Custom) && package.UsedBy.Any(i => i.Name.EqualsIgnoreCase(AppName)))
                {
                    _appPackages.Add(id);
                }
            }
        }

        public bool PackageExists(LibraryId libraryId) => _infoById.ContainsKey(libraryId);

        public string GetPackageLicenseCode(LibraryId libraryId)
        {
            _infoById.TryGetValue(libraryId, out var info);
            return info.LicenseCode;
        }

        public bool IsAssignedToApp(LibraryId libraryId)
        {
            return _appPackages.Remove(libraryId);
        }

        public bool GetLicenseRequiresApproval(LibraryId libraryId)
        {
            var info = _infoById[libraryId];
            var codes = LicenseExpression.GetCodes(info.LicenseCode);

            foreach (var code in codes)
            {
                if (_licenseByCode[code].RequiresApproval)
                {
                    return true;
                }
            }

            return false;
        }

        public bool GetLicenseRequiresThirdPartyNotices(LibraryId libraryId)
        {
            var info = _infoById[libraryId];
            var codes = LicenseExpression.GetCodes(info.LicenseCode);

            foreach (var code in codes)
            {
                if (_licenseByCode[code].RequiresThirdPartyNotices)
                {
                    return true;
                }
            }

            return false;
        }

        public PackageApprovalStatus GetPackageApprovalStatus(LibraryId libraryId)
        {
            _infoById.TryGetValue(libraryId, out var info);
            return info.Status;
        }

        public bool GetPackageHasThirdPartyNotices(LibraryId libraryId)
        {
            _infoById.TryGetValue(libraryId, out var info);
            return info.HasThirdPartyNotices;
        }

        public IEnumerable<LibraryId> GetAppTrash()
        {
            return _appPackages;
        }

        private async Task LoadLicenseAsync(string licenseExpression, CancellationToken token)
        {
            if (licenseExpression.IsNullOrEmpty())
            {
                return;
            }

            var codes = LicenseExpression.GetCodes(licenseExpression);
            
            foreach (var code in codes)
            {
                if (!_licenseByCode.ContainsKey(code))
                {
                    var index = await Repository.Storage.ReadLicenseIndexJsonAsync(code, token).ConfigureAwait(false);
                    _licenseByCode.Add(code, index);
                }
            }
        }
    }
}
