using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal sealed class NuGetPackageRepositoryAdapter : IPackageRepositoryAdapter
    {
        [Dependency]
        public NuGetConfiguration Configuration { get; set; }

        [Dependency]
        public INuGetApi Api { get; set; }

        [Dependency]
        public IStorage Storage { get; set; }

        public async Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token)
        {
            var (spec, index) = await RepositoryReadAsync(id, token);
            if (spec == null)
            {
                return null;
            }

            var result = NuGetConstants.CreatePackage(spec, index.License.Code, index.License.Status);
            result.UsedBy = index.UsedBy.Select(i => i.Name).ToArray();

            foreach (var license in index.Licenses)
            {
                result.Licenses.Add(new PackageLicense
                {
                    Code = license.Code,
                    HRef = license.HRef,
                    Subject = license.Subject,
                    CodeDescription = license.Description
                });
            }

            return result;
        }

        public async Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token)
        {
            reference.AssertNotNull(nameof(reference));
            package.AssertNotNull(nameof(package));
            appName.AssertNotNull(nameof(appName));

            var model = await Storage.ReadLibraryIndexJsonAsync<NuGetLibraryIndexJson>(reference.Id, token);
            
            if (model == null)
            {
                model = new NuGetLibraryIndexJson();
            }

            model.License.Code = package.LicenseCode;
            model.License.Status = package.ApprovalStatus.ToString();

            model.Licenses.Clear();
            foreach (var license in package.Licenses)
            {
                model.Licenses.Add(new LibraryLicense { Code = license.Code, Description = license.CodeDescription, HRef = license.HRef, Subject = license.Subject });
            }

            var app = model.UsedBy.FirstOrDefault(i => appName.EqualsIgnoreCase(i.Name));
            if (app == null)
            {
                app = new Application { Name = appName };
                model.UsedBy.Add(app);
            }

            app.InternalOnly = reference.IsInternal;
            app.TargetFrameworks = reference.TargetFrameworks;
            app.Dependencies.Clear();
            foreach (var dependency in reference.Dependencies)
            {
                app.Dependencies.Add(new LibraryDependency { Name = dependency.Name, Version = dependency.Version });
            }

            foreach (var attachment in package.Attachments)
            {
                await Storage.WriteLibraryFileAsync(reference.Id, attachment.Name, attachment.Content, token);
            }

            if (Configuration.DownloadPackageIntoRepository && !await Storage.LibraryFileExistsAsync(reference.Id, NuGetConstants.RepositoryPackageFileName, token))
            {
                var content = await Api.LoadPackageAsync(new NuGetPackageId(reference.Id.Name, reference.Id.Version), Configuration.AllowToUseLocalCache, token);
                if (content == null)
                {
                    throw new InvalidOperationException("Package not found on www.nuget.org.");
                }

                await Storage.WriteLibraryFileAsync(reference.Id, NuGetConstants.RepositoryPackageFileName, content, token);
            }

            await Storage.WriteLibraryIndexJsonAsync(reference.Id, model, token);
        }

        public async Task<PackageReadMe> UpdatePackageReadMeAsync(LibraryId id, CancellationToken token)
        {
            var (spec, index) = await RepositoryReadAsync(id, token);

            var context = CreateReadMeContext(spec, index);
            context.ThirdPartyNotices = await LoadThirdPartyNoticesAsync(id, true, token);

            using (var stream = await Storage.OpenLibraryFileReadAsync(id, NuGetConstants.RepositoryRemarksFileName, CancellationToken.None))
            {
                if (stream == null)
                {
                    context.Remarks = "no remarks";
                    await Storage.WriteLibraryFileAsync(id, NuGetConstants.RepositoryRemarksFileName, Encoding.UTF8.GetBytes(context.Remarks), token);
                }
                else
                {
                    using (var reader = new StreamReader(stream))
                    {
                        context.Remarks = reader.ReadToEnd();
                    }   
                }
            }

            var metadata = new PackageReadMe
            {
                SourceCode = PackageSources.NuGet,
                Name = context.Name,
                Version = context.Version,
                HRef = context.HRef,
                LicenseCode = context.LicenseCode,
                ApprovalStatus = Enum.Parse<PackageApprovalStatus>(index.License.Status),
                UsedBy = context.UsedBy
            };

            if (context.LicenseCode.IsNullOrEmpty())
            {
                context.LicenseCode = "Unknown";
            }

            await Storage.WriteLibraryReadMeAsync(id, context, token);

            return metadata;
        }

        public async Task<PackageNotices> LoadPackageNoticesAsync(LibraryId id, CancellationToken token)
        {
            var (spec, index) = await RepositoryReadAsync(id, token);

            return new PackageNotices
            {
                Name = spec.Id,
                Version = spec.Version,
                LicenseCode = index.License.Code,
                HRef = spec.PackageHRef,
                Author = spec.Authors,
                Copyright = spec.Copyright,
                UsedBy = index.UsedBy.Select(i => new PackageNoticesApplication(i.Name, i.InternalOnly)).ToArray(),
                ThirdPartyNotices = await LoadThirdPartyNoticesAsync(id, false, token)
            };
        }

        public async ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token)
        {
            appName.AssertNotNull(nameof(appName));

            var model = await Storage.ReadLibraryIndexJsonAsync<NuGetLibraryIndexJson>(id, token);
            var result = PackageRemoveResult.None;

            var index = model.UsedBy.IndexOf(i => i.Name.EqualsIgnoreCase(appName));
            if (index >= 0)
            {
                model.UsedBy.RemoveAt(index);
                await Storage.WriteLibraryIndexJsonAsync(id, model, token);

                result = model.UsedBy.Count == 0 ? PackageRemoveResult.RemovedNoRefs : PackageRemoveResult.Removed;
            }

            return result;
        }

        private async Task<(NuGetSpec Spec, NuGetLibraryIndexJson Index)> RepositoryReadAsync(LibraryId id, CancellationToken token)
        {
            var indexTask = Storage.ReadLibraryIndexJsonAsync<NuGetLibraryIndexJson>(id, CancellationToken.None);

            NuGetSpec spec = null;
            using (var specContent = await Storage.OpenLibraryFileReadAsync(id, NuGetConstants.RepositorySpecFileName, token))
            {
                if (specContent != null)
                {
                    spec = Api.ParseSpec(specContent);
                }
            }

            var index = await indexTask;

            return (spec, index);
        }

        private NuGetReadMeContext CreateReadMeContext(NuGetSpec spec, NuGetLibraryIndexJson index)
        {
            var context = new NuGetReadMeContext
            {
                Name = spec.Id,
                Version = spec.Version,
                HRef = spec.PackageHRef,
                LicenseCode = index.License.Code,
                UsedBy = PackageReadMe.BuildUsedBy(index.UsedBy),
                TargetFrameworks = string.Join(", ", index.UsedBy.SelectMany(i => i.TargetFrameworks).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(i => i)),
                Description = spec.Description
            };

            if (!context.LicenseCode.IsNullOrEmpty())
            {
                var codes = LicenseExpression.GetCodes(context.LicenseCode);
                var relativeTo = new LibraryId(PackageSources.NuGet, spec.Id, spec.Version);

                context.LicenseLocalHRef = Storage.GetLicenseLocalHRef(codes.First(), relativeTo);
                context.LicenseMarkdownExpression = LicenseExpression.ReplaceCodes(
                    context.LicenseCode,
                    i => "[{0}]({1})".FormatWith(i, Storage.GetLicenseLocalHRef(i, relativeTo)));

                if (Enum.Parse<PackageApprovalStatus>(index.License.Status) == PackageApprovalStatus.HasToBeApproved)
                {
                    context.LicenseDescription = "has to be approved";
                }
            }

            foreach (var license in index.Licenses)
            {
                if (license.Code.IsNullOrEmpty())
                {
                    license.Code = "Unknown";
                }

                context.Licenses.Add(license);
            }

            var dependencies = index
                .UsedBy
                .SelectMany(i => i.Dependencies)
                .Select(i => new LibraryId(PackageSources.NuGet, i.Name, i.Version))
                .Distinct();
            foreach (var dependency in dependencies)
            {
                context.Dependencies.Add(new NuGetReadMeDependencyContext
                {
                    Name = dependency.Name,
                    Version = dependency.Version,
                    LocalHRef = Storage.GetPackageLocalHRef(dependency, new LibraryId(PackageSources.NuGet, spec.Id, spec.Version))
                });
            }

            return context;
        }

        private async Task<string> LoadThirdPartyNoticesAsync(LibraryId id, bool createEmpty, CancellationToken token)
        {
            string result = null;

            using (var stream = await Storage.OpenLibraryFileReadAsync(id, NuGetConstants.RepositoryThirdPartyNoticesFileName, token))
            {
                if (stream == null && createEmpty)
                {
                    await Storage.WriteLibraryFileAsync(id, NuGetConstants.RepositoryThirdPartyNoticesFileName, Array.Empty<byte>(), token);
                }

                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }

            return result.IsNullOrEmpty() ? null : result;
        }
    }
}
