using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using Unity;
using Unity.Resolution;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal sealed class PackageRepository : IPackageRepository
    {
        public PackageRepository(IUnityContainer container, IStorage storage)
        {
            container.AssertNotNull(nameof(container));
            storage.AssertNotNull(nameof(storage));

            Container = container;
            Storage = storage;
        }

        public IUnityContainer Container { get; }

        public IStorage Storage { get; }
        
        public Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token)
        {
            return ResolveAdapter(id.SourceCode).LoadPackageAsync(id, token);
        }

        public Task<PackageNotices> LoadPackagesNoticesAsync(LibraryId id, CancellationToken token)
        {
            return ResolveAdapter(id.SourceCode).LoadPackageNoticesAsync(id, token);
        }

        public Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token)
        {
            package.AssertNotNull(nameof(package));

            return ResolveAdapter(package.SourceCode).UpdatePackageAsync(reference, package, appName, token);
        }

        public async Task<RepositoryLicense> LoadOrCreateLicenseAsync(string licenseCode, CancellationToken token)
        {
            licenseCode.AssertNotNull(nameof(licenseCode));

            var index = await Storage.ReadLicenseIndexJsonAsync(licenseCode, token);
            if (index != null)
            {
                return new RepositoryLicense(index.Code, index.RequiresApproval, index.RequiresThirdPartyNotices, index.Dependencies);
            }

            var info = await Container.Resolve<ILicenseResolver>().DownloadByCodeAsync(licenseCode, token);
            if (info == null)
            {
                info = new LicenseInfo
                {
                    Code = licenseCode,
                    FileName = "license.txt",
                    FileContent = Array.Empty<byte>()
                };
            }

            index = new LicenseIndexJson
            {
                Code = info.Code,
                FullName = info.FullName,
                HRef = info.FileHRef,
                FileName = info.FileName,
                RequiresApproval = true,
                RequiresThirdPartyNotices = false
            };
            await Storage.CreateLicenseIndexJsonAsync(index, token);
            await Storage.CreateLicenseFileAsync(info.Code, info.FileName, info.FileContent, token);

            return new RepositoryLicense(index.Code, index.RequiresApproval, index.RequiresThirdPartyNotices, index.Dependencies);
        }

        public async Task<IList<PackageReadMe>> UpdateAllPackagesReadMeAsync(CancellationToken token)
        {
            var libraries = await Storage.GetAllLibrariesAsync(token);
            var result = new List<PackageReadMe>(libraries.Count);

            var librariesBySourceCode = libraries.GroupBy(i => i.SourceCode, StringComparer.OrdinalIgnoreCase);
            foreach (var entry in librariesBySourceCode)
            {
                var adapter = ResolveAdapter(entry.Key);
                foreach (var id in entry)
                {
                    result.Add(await adapter.UpdatePackageReadMeAsync(id, token));
                }
            }

            return result;
        }

        public async ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token)
        {
            var result = await ResolveAdapter(id.SourceCode).RemoveFromApplicationAsync(id, appName, token);
            if (result == PackageRemoveResult.RemovedNoRefs)
            {
                await Storage.RemoveLibraryAsync(id, token);
            }

            return result;
        }

        internal IPackageRepositoryAdapter ResolveAdapter(string sourceCode)
        {
            return Container.Resolve<IPackageRepositoryAdapter>(
                sourceCode,
                new PropertyOverride(nameof(IPackageRepositoryAdapter.Storage), Storage));
        }
    }
}
