using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Npm;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters
{
    internal sealed class NpmPackageRepositoryAdapter : PackageRepositoryAdapterBase
    {
        [Dependency]
        public INpmApi Api { get; set; }

        protected override async Task AppendSpecAttributesAsync(LibraryId id, Package package, CancellationToken token)
        {
            var json = await ReadPackageJsonAsync(id, token);
         
            // no Copyright
            package.Name = json.Name;
            package.Version = json.Version;
            package.Description = json.Description;
            package.HRef = json.PackageHRef;
            package.Author = json.Authors;
        }

        private async Task<PackageJson> ReadPackageJsonAsync(LibraryId id, CancellationToken token)
        {
            using (var jsonContent = await Storage.OpenLibraryFileReadAsync(id, NpmConstants.RepositoryPackageJsonFileName, token))
            {
                return Api.ParsePackageJson(jsonContent);
            }
        }
    }
}
