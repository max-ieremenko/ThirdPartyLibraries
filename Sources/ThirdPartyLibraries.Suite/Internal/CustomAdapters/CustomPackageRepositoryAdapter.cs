using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;

namespace ThirdPartyLibraries.Suite.Internal.CustomAdapters;

internal sealed class CustomPackageRepositoryAdapter : IPackageRepositoryAdapter
{
    public IStorage Storage { get; set; }

    public async Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token)
    {
        var index = await Storage.ReadLibraryIndexJsonAsync<CustomLibraryIndexJson>(id, token).ConfigureAwait(false);
        var package = new Package
        {
            SourceCode = PackageSources.Custom,
            Name = index.Name,
            Version = index.Version,
            LicenseCode = index.LicenseCode,
            Author = index.Author,
            Copyright = index.Copyright,
            HRef = index.HRef,
            HRefText = PackageSources.Custom,
            UsedBy = index.UsedBy.Select(i => new PackageApplication(i.Name, i.InternalOnly)).ToArray(),
            ApprovalStatus = PackageApprovalStatus.Approved
        };

        package.ThirdPartyNotices = await Storage.ReadThirdPartyNoticesFile(id, token).ConfigureAwait(false);
        return package;
    }

    public Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token)
    {
        throw new NotSupportedException();
    }

    public Task UpdatePackageReadMeAsync(Package package, CancellationToken token)
    {
        package.AssertNotNull(nameof(package));

        var id = new LibraryId(package.SourceCode, package.Name, package.Version);
        return Storage.CreateDefaultThirdPartyNoticesFile(id, token);
    }

    public ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token)
    {
        // custom packages cannot be removed automatically
        return new ValueTask<PackageRemoveResult>(PackageRemoveResult.None);
    }
}