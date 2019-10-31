using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands
{
    internal sealed class ValidateCommandState
    {
        private readonly HashSet<LibraryId> _appPackages;
        private readonly IDictionary<LibraryId, (string LicenseCode, PackageApprovalStatus Status)> _infoById;

        public ValidateCommandState(IPackageRepository repository, string appName)
        {
            Repository = repository;
            AppName = appName;

            _appPackages = new HashSet<LibraryId>();
            _infoById = new Dictionary<LibraryId, (string LicenseCode, PackageApprovalStatus Status)>();
        }

        public IPackageRepository Repository { get; }
        
        public string AppName { get; }

        public async Task InitializeAsync(CancellationToken token)
        {
            var libraries = await Repository.Storage.GetAllLibrariesAsync(token);
            foreach (var id in libraries)
            {
                var package = await Repository.LoadPackageAsync(id, token);

                _infoById.Add(id, (package.LicenseCode, package.ApprovalStatus));

                if (!package.SourceCode.Equals(PackageSources.Custom) && package.UsedBy.Any(i => i.EqualsIgnoreCase(AppName)))
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

        public PackageApprovalStatus GetPackageApprovalStatus(LibraryId libraryId)
        {
            _infoById.TryGetValue(libraryId, out var info);
            return info.Status;
        }

        public IEnumerable<LibraryId> GetAppTrash()
        {
            return _appPackages;
        }
    }
}
