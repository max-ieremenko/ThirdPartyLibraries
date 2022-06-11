using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Npm;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters
{
    internal sealed class NpmPackageResolver : PackageResolverBase
    {
        public NpmPackageResolver(INpmApi api, NpmConfiguration configuration, ILicenseResolver licenseResolver, IStorage storage)
            : base(licenseResolver, storage)
        {
            Api = api;
            Configuration = configuration;
        }

        public INpmApi Api { get; }

        public NpmConfiguration Configuration { get; }

        protected override bool DownloadPackageIntoRepository => Configuration.DownloadPackageIntoRepository;

        protected override string RepositoryPackageFileName => NpmConstants.RepositoryPackageFileName;

        protected override async Task CreateNewAsync(LibraryId id, LibraryIndexJson index, CancellationToken token)
        {
            var package = await GetPackageContentAsync(id, token).ConfigureAwait(false);

            var specContent = Api.ExtractPackageJson(package);
            await Storage.WriteLibraryFileAsync(id, NpmConstants.RepositoryPackageJsonFileName, specContent, token).ConfigureAwait(false);

            var spec = Api.ParsePackageJson(specContent);

            index.Licenses.Add(await ResolvePackageLicenseAsync(id, spec.License?.Type, spec.License?.Value, null, token).ConfigureAwait(false));

            if (spec.Repository?.Url != null)
            {
                index.Licenses.Add(await ResolveUrlLicenseAsync(id, spec.Repository.Url, PackageLicense.SubjectRepository, token).ConfigureAwait(false));
            }

            if (!spec.HomePage.IsNullOrEmpty())
            {
                index.Licenses.Add(await ResolveUrlLicenseAsync(id, spec.HomePage, PackageLicense.SubjectHomePage, token).ConfigureAwait(false));
            }
        }

        protected override async Task<byte[]> DownloadPackageContentAsync(LibraryId id, CancellationToken token)
        {
            var file = await Api.DownloadPackageAsync(new NpmPackageId(id.Name, id.Version), token).ConfigureAwait(false);
            return file?.Content;
        }

        protected override Task<byte[]> GetPackageFileContentAsync(byte[] package, string fileName, CancellationToken token)
        {
            var content = Api.LoadFileContent(package, fileName);
            return Task.FromResult(content);
        }

        protected override string[] FindPackageFiles(byte[] package, string searchPattern)
        {
            return Api.FindFiles(package, searchPattern);
        }
    }
}
