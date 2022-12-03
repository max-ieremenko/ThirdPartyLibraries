using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Npm;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters;

internal sealed class NpmPackageRepositoryAdapter : PackageRepositoryAdapterBase
{
    public NpmPackageRepositoryAdapter(INpmApi api)
    {
        Api = api;
    }

    public INpmApi Api { get; }

    protected override async Task AppendSpecAttributesAsync(LibraryId id, Package package, CancellationToken token)
    {
        var json = await ReadPackageJsonAsync(id, token).ConfigureAwait(false);
         
        // no Copyright
        package.Name = json.Name;
        package.Version = json.Version;
        package.Description = json.Description;
        package.HRef = json.PackageHRef;
        package.Author = json.Authors;
    }

    private async Task<PackageJson> ReadPackageJsonAsync(LibraryId id, CancellationToken token)
    {
        using (var jsonContent = await Storage.OpenLibraryFileReadAsync(id, NpmConstants.RepositoryPackageJsonFileName, token).ConfigureAwait(false))
        {
            return Api.ParsePackageJson(jsonContent);
        }
    }
}