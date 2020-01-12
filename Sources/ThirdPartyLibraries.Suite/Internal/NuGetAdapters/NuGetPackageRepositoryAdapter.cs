using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal sealed class NuGetPackageRepositoryAdapter : PackageRepositoryAdapterBase
    {
        [Dependency]
        public INuGetApi Api { get; set; }

        protected override async Task AppendSpecAttributesAsync(LibraryId id, Package package, CancellationToken token)
        {
            var spec = await ReadSpecAsync(id, token);
            package.Name = spec.Id;
            package.Version = spec.Version;
            package.Description = spec.Description;
            package.HRef = spec.PackageHRef;
            package.Author = spec.Authors;
            package.Copyright = spec.Copyright;
        }

        private async Task<NuGetSpec> ReadSpecAsync(LibraryId id, CancellationToken token)
        {
            using (var specContent = await Storage.OpenLibraryFileReadAsync(id, NuGetConstants.RepositorySpecFileName, token))
            {
                return Api.ParseSpec(specContent);
            }
        }
    }
}
