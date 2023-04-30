using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters;

internal sealed class NuGetPackageResolver : PackageResolverBase
{
    public NuGetPackageResolver(NuGetConfiguration configuration, INuGetApi api, ILicenseResolver licenseResolver, IStorage storage)
        : base(licenseResolver, storage)
    {
        Configuration = configuration;
        Api = api;
    }

    public NuGetConfiguration Configuration { get; }

    public INuGetApi Api { get; }

    protected override bool DownloadPackageIntoRepository => Configuration.DownloadPackageIntoRepository;

    protected override string RepositoryPackageFileName => NuGetConstants.RepositoryPackageFileName;

    protected override async Task CreateNewAsync(LibraryId id, LibraryIndexJson index, CancellationToken token)
    {
        var package = await GetPackageContentAsync(id, token).ConfigureAwait(false);

        var specContent = await Api.ExtractSpecAsync(new NuGetPackageId(id.Name, id.Version), package, token).ConfigureAwait(false);
        await Storage.WriteLibraryFileAsync(id, NuGetConstants.RepositorySpecFileName, specContent, token).ConfigureAwait(false);

        var spec = Api.ParseSpec(specContent);

        var specLicenseUrl = NuGetConstants.IsDeprecateLicenseUrl(spec.LicenseUrl) ? null : spec.LicenseUrl;
        index.Licenses.Add(await ResolvePackageLicenseAsync(id, spec.License?.Type, spec.License?.Value, specLicenseUrl, token).ConfigureAwait(false));

        if (!string.IsNullOrEmpty(spec.Repository?.Url))
        {
            index.Licenses.Add(await ResolveUrlLicenseAsync(id, spec.Repository.Url, PackageLicense.SubjectRepository, token).ConfigureAwait(false));
        }

        if (!spec.ProjectUrl.IsNullOrEmpty())
        {
            index.Licenses.Add(await ResolveUrlLicenseAsync(id, spec.ProjectUrl, PackageLicense.SubjectProject, token).ConfigureAwait(false));
        }

        index.Source = Api.ResolvePackageSource(new NuGetPackageId(id.Name, id.Version));
    }

    protected override Task<byte[]> DownloadPackageContentAsync(LibraryId id, CancellationToken token)
    {
        return Api.DownloadPackageAsync(new NuGetPackageId(id.Name, id.Version), Configuration.AllowToUseLocalCache, token);
    }

    protected override Task<byte[]> GetPackageFileContentAsync(byte[] package, string fileName, CancellationToken token)
    {
        return Api.LoadFileContentAsync(package, fileName, token);
    }

    protected override string[] FindPackageFiles(byte[] package, string searchPattern)
    {
        return Api.FindFiles(package, searchPattern);
    }
}