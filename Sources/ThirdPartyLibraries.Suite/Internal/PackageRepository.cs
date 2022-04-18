using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal sealed class PackageRepository : IPackageRepository
    {
        public PackageRepository(IServiceProvider serviceProvider, IStorage storage)
        {
            serviceProvider.AssertNotNull(nameof(serviceProvider));
            storage.AssertNotNull(nameof(storage));

            ServiceProvider = serviceProvider;
            Storage = storage;
        }

        public IServiceProvider ServiceProvider { get; }

        public IStorage Storage { get; }

        public Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token)
        {
            return ResolveAdapter(id.SourceCode).LoadPackageAsync(id, token);
        }

        public Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token)
        {
            package.AssertNotNull(nameof(package));

            return ResolveAdapter(package.SourceCode).UpdatePackageAsync(reference, package, appName, token);
        }

        public async Task<RepositoryLicense> LoadOrCreateLicenseAsync(string licenseCode, CancellationToken token)
        {
            licenseCode.AssertNotNull(nameof(licenseCode));

            var index = await Storage.ReadLicenseIndexJsonAsync(licenseCode, token).ConfigureAwait(false);
            if (index != null)
            {
                return new RepositoryLicense(index.Code, index.RequiresApproval, index.RequiresThirdPartyNotices, index.Dependencies);
            }

            var info = await ServiceProvider.GetRequiredService<ILicenseResolver>().DownloadByCodeAsync(licenseCode, token).ConfigureAwait(false);
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
            await Storage.CreateLicenseIndexJsonAsync(index, token).ConfigureAwait(false);
            await Storage.CreateLicenseFileAsync(info.Code, info.FileName, info.FileContent, token).ConfigureAwait(false);

            return new RepositoryLicense(index.Code, index.RequiresApproval, index.RequiresThirdPartyNotices, index.Dependencies);
        }

        public async Task<IList<Package>> UpdateAllPackagesReadMeAsync(CancellationToken token)
        {
            var libraries = await Storage.GetAllLibrariesAsync(token).ConfigureAwait(false);
            var result = new List<Package>(libraries.Count);

            var librariesBySourceCode = libraries.GroupBy(i => i.SourceCode, StringComparer.OrdinalIgnoreCase);
            foreach (var entry in librariesBySourceCode)
            {
                var adapter = ResolveAdapter(entry.Key);
                foreach (var id in entry)
                {
                    var package = await adapter.LoadPackageAsync(id, token).ConfigureAwait(false);
                    await adapter.UpdatePackageReadMeAsync(package, token).ConfigureAwait(false);
                    result.Add(package);
                }
            }

            return result;
        }

        public async ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token)
        {
            var result = await ResolveAdapter(id.SourceCode).RemoveFromApplicationAsync(id, appName, token).ConfigureAwait(false);
            if (result == PackageRemoveResult.RemovedNoRefs)
            {
                await Storage.RemoveLibraryAsync(id, token).ConfigureAwait(false);
            }

            return result;
        }

        internal IPackageRepositoryAdapter ResolveAdapter(string sourceCode)
        {
            var adapter = ServiceProvider.GetRequiredKeyedService<IPackageRepositoryAdapter>(sourceCode);
            adapter.Storage = Storage;
            return adapter;
        }
    }
}
