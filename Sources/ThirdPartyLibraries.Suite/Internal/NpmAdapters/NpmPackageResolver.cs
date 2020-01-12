using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Npm;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters
{
    internal sealed class NpmPackageResolver : PackageResolverBase
    {
        [Dependency]
        public INpmApi Api { get; set; }

        [Dependency]
        public NpmConfiguration Configuration { get; set; }

        protected override bool DownloadPackageIntoRepository => Configuration.DownloadPackageIntoRepository;

        protected override string RepositoryPackageFileName => NpmConstants.RepositoryPackageFileName;

        protected override async Task CreateNewAsync(LibraryId id, LibraryIndexJson index, CancellationToken token)
        {
            var package = await GetPackageContentAsync(id, token);

            var specContent = await Api.ExtractPackageJsonAsync(package, token);
            await Storage.WriteLibraryFileAsync(id, NpmConstants.RepositoryPackageJsonFileName, specContent, token);

            var spec = Api.ParsePackageJson(specContent);

            index.Licenses.Add(await ResolvePackageLicenseAsync(id, spec.License?.Type, spec.License?.Value, null, token));

            if (spec.Repository?.Url != null)
            {
                index.Licenses.Add(await ResolveUrlLicenseAsync(id, spec.Repository.Url, PackageLicense.SubjectRepository, token));
            }

            if (!spec.HomePage.IsNullOrEmpty())
            {
                index.Licenses.Add(await ResolveUrlLicenseAsync(id, spec.HomePage, PackageLicense.SubjectHomePage, token));
            }
        }

        protected override async Task<byte[]> DownloadPackageContentAsync(LibraryId id, CancellationToken token)
        {
            var file = await Api.DownloadPackageAsync(new NpmPackageId(id.Name, id.Version), token);
            return file?.Content;
        }

        protected override async Task<byte[]> GetPackageFileContentAsync(LibraryId id, string fileName, CancellationToken token)
        {
            var package = await GetPackageContentAsync(id, token);
            return await Api.LoadFileContentAsync(package, fileName, token);
        }
    }
}
