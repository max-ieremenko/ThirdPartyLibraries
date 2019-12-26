using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Npm;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.NuGetAdapters;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters
{
    internal sealed class NpmPackageRepositoryAdapter : IPackageRepositoryAdapter
    {
        [Dependency]
        public NpmConfiguration Configuration { get; set; }

        [Dependency]
        public IStorage Storage { get; set; }

        [Dependency]
        public INpmApi Api { get; set; }

        public async Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token)
        {
            var (json, index) = await RepositoryReadAsync(id, token);
            if (json == null)
            {
                return null;
            }

            var package = NpmConstants.CreatePackage(json, index.License.Code, index.License.Status);

            foreach (var license in index.Licenses)
            {
                package.Licenses.Add(new PackageLicense
                {
                    Code = license.Code,
                    HRef = license.HRef,
                    Subject = license.Subject,
                    CodeDescription = license.Description
                });
            }

            return package;
        }

        public async Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token)
        {
            reference.AssertNotNull(nameof(reference));
            package.AssertNotNull(nameof(package));
            appName.AssertNotNull(nameof(appName));

            var model = await Storage.ReadLibraryIndexJsonAsync<NpmLibraryIndexJson>(reference.Id, token);

            if (model == null)
            {
                model = new NpmLibraryIndexJson();
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
                app = new NpmApplication { Name = appName };
                model.UsedBy.Add(app);
            }

            app.InternalOnly = reference.IsInternal;

            foreach (var attachment in package.Attachments)
            {
                await Storage.WriteLibraryFileAsync(reference.Id, attachment.Name, attachment.Content, token);
            }

            if (Configuration.DownloadPackageIntoRepository && !await Storage.LibraryFileExistsAsync(reference.Id, NpmConstants.RepositoryPackageFileName, token))
            {
                var content = await Api.DownloadPackageAsync(new NpmPackageId(reference.Id.Name, reference.Id.Version), token);
                if (content == null)
                {
                    throw new InvalidOperationException("Package not found on www.nuget.org.");
                }

                await Storage.WriteLibraryFileAsync(reference.Id, NpmConstants.RepositoryPackageFileName, content.Value.Content, token);
            }

            await Storage.WriteLibraryIndexJsonAsync(reference.Id, model, token);
        }

        public async Task<PackageReadMe> UpdatePackageReadMeAsync(LibraryId id, CancellationToken token)
        {
            var (json, index) = await RepositoryReadAsync(id, token);

            var context = CreateReadMeContext(json, index);
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
            var (json, index) = await RepositoryReadAsync(id, token);

            return new PackageNotices
            {
                Name = json.Name,
                Version = json.Version,
                LicenseCode = index.License.Code,
                HRef = json.PackageHRef,
                Author = json.Authors,
                UsedBy = index.UsedBy.Select(i => new PackageNoticesApplication(i.Name, i.InternalOnly)).ToArray(),
                ThirdPartyNotices = await LoadThirdPartyNoticesAsync(id, false, token)
            };
        }

        public async ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token)
        {
            appName.AssertNotNull(nameof(appName));

            var model = await Storage.ReadLibraryIndexJsonAsync<NpmLibraryIndexJson>(id, token);
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

        private async Task<(PackageJson Json, NpmLibraryIndexJson Index)> RepositoryReadAsync(LibraryId id, CancellationToken token)
        {
            var indexTask = Storage.ReadLibraryIndexJsonAsync<NpmLibraryIndexJson>(id, CancellationToken.None);

            PackageJson json = null;
            using (var jsonContent = await Storage.OpenLibraryFileReadAsync(id, NpmConstants.RepositoryPackageJsonFileName, token))
            {
                if (jsonContent != null)
                {
                    json = Api.ParsePackageJson(jsonContent);
                }
            }

            var index = await indexTask;

            return (json, index);
        }

        private NpmReadMeContext CreateReadMeContext(PackageJson json, NpmLibraryIndexJson index)
        {
            var context = new NpmReadMeContext
            {
                Name = json.Name,
                Version = json.Version,
                HRef = json.PackageHRef,
                LicenseCode = index.License.Code,
                UsedBy = PackageReadMe.BuildUsedBy(index.UsedBy.Select(i => (i.Name, i.InternalOnly))),
                Description = json.Description
            };

            if (!context.LicenseCode.IsNullOrEmpty())
            {
                var codes = LicenseExpression.GetCodes(context.LicenseCode);

                context.LicenseLocalHRef = Storage.GetLicenseLocalHRef(codes.First(), RelativeTo.Library);
                context.LicenseMarkdownExpression = LicenseExpression.ReplaceCodes(
                    context.LicenseCode,
                    i => "[{0}]({1})".FormatWith(i, Storage.GetLicenseLocalHRef(i, RelativeTo.Library)));

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

            return context;
        }

        private async Task<string> LoadThirdPartyNoticesAsync(LibraryId id, bool createEmpty, CancellationToken token)
        {
            string result = null;

            using (var stream = await Storage.OpenLibraryFileReadAsync(id, NpmConstants.RepositoryThirdPartyNoticesFileName, token))
            {
                if (stream == null && createEmpty)
                {
                    await Storage.WriteLibraryFileAsync(id, NpmConstants.RepositoryThirdPartyNoticesFileName, Array.Empty<byte>(), token);
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
