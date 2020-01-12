using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal sealed class NuGetPackageResolver : PackageResolverBase
    {
        [Dependency]
        public NuGetConfiguration Configuration { get; set; }

        [Dependency]
        public INuGetApi Api { get; set; }

        protected override bool DownloadPackageIntoRepository => Configuration.DownloadPackageIntoRepository;

        protected override string RepositoryPackageFileName => NuGetConstants.RepositoryPackageFileName;

        protected override async Task CreateNewAsync(LibraryId id, LibraryIndexJson index, CancellationToken token)
        {
            var package = await GetPackageContentAsync(id, token);

            var specContent = await Api.ExtractSpecAsync(new NuGetPackageId(id.Name, id.Version), package, token);
            await Storage.WriteLibraryFileAsync(id, NuGetConstants.RepositorySpecFileName, specContent, token);

            var spec = Api.ParseSpec(specContent);

            var specLicenseUrl = IsDeprecateLicenseUrl(spec.LicenseUrl) ? null : spec.LicenseUrl;
            index.Licenses.Add(await ResolvePackageLicenseAsync(id, spec.License?.Type, spec.License?.Value, specLicenseUrl, token));

            if (spec.Repository?.Url != null)
            {
                index.Licenses.Add(await ResolveUrlLicenseAsync(id, spec.Repository.Url, PackageLicense.SubjectRepository, token));
            }

            if (!spec.ProjectUrl.IsNullOrEmpty())
            {
                index.Licenses.Add(await ResolveUrlLicenseAsync(id, spec.ProjectUrl, PackageLicense.SubjectProject, token));
            }
        }

        protected override Task<byte[]> DownloadPackageContentAsync(LibraryId id, CancellationToken token)
        {
            return Api.DownloadPackageAsync(new NuGetPackageId(id.Name, id.Version), Configuration.AllowToUseLocalCache, token);
        }

        protected override async Task<byte[]> GetPackageFileContentAsync(LibraryId id, string fileName, CancellationToken token)
        {
            var package = await GetPackageContentAsync(id, token);
            return await Api.LoadFileContentAsync(package, fileName, token);
        }

        private static bool IsDeprecateLicenseUrl(string value)
        {
            // https://aka.ms/deprecateLicenseUrl
            if (value.IsNullOrEmpty() || !Uri.TryCreate(value, UriKind.Absolute, out var url))
            {
                return false;
            }

            return url.Host.EqualsIgnoreCase("aka.ms") && url.LocalPath.StartsWithIgnoreCase("deprecateLicenseUrl");
        }
    }
}
