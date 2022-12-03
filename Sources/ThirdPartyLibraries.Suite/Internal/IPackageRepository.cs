using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Suite.Internal;

internal interface IPackageRepository
{
    IStorage Storage { get; }

    Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token);

    Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token);

    Task<RepositoryLicense> LoadOrCreateLicenseAsync(string licenseCode, CancellationToken token);

    Task<IList<Package>> UpdateAllPackagesReadMeAsync(CancellationToken token);

    ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token);
}