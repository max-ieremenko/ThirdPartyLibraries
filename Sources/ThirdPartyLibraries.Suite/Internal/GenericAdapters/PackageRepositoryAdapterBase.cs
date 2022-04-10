using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.GenericAdapters
{
    internal abstract class PackageRepositoryAdapterBase : IPackageRepositoryAdapter
    {
        public IStorage Storage { get; set; }

        public async Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token)
        {
            var index = await Storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(id, token).ConfigureAwait(false);

            var package = new Package
            {
                SourceCode = id.SourceCode,
                LicenseCode = index.License.Code,
                UsedBy = index.UsedBy.Select(i => new PackageApplication(i.Name, i.InternalOnly)).ToArray()
            };

            if (!index.License.Status.IsNullOrEmpty())
            {
                package.ApprovalStatus = Enum.Parse<PackageApprovalStatus>(index.License.Status);
            }

            package.Remarks = await Storage.ReadRemarksFileName(id, token).ConfigureAwait(false);
            package.ThirdPartyNotices = await Storage.ReadThirdPartyNoticesFile(id, token).ConfigureAwait(false);

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

            await AppendSpecAttributesAsync(id, package, token).ConfigureAwait(false);
            return package;
        }

        public async Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token)
        {
            reference.AssertNotNull(nameof(reference));
            package.AssertNotNull(nameof(package));
            appName.AssertNotNull(nameof(appName));

            var index = await Storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(reference.Id, token).ConfigureAwait(false);

            // licenses are updated by PackageResolver
            // only ApprovalStatus is managed by UpdateCommand
            index.License.Status = package.ApprovalStatus.ToString();

            var app = index.UsedBy.FirstOrDefault(i => appName.EqualsIgnoreCase(i.Name));
            if (app == null)
            {
                app = new Application { Name = appName };
                index.UsedBy.Add(app);
            }

            app.InternalOnly = reference.IsInternal;
            app.TargetFrameworks = reference.TargetFrameworks;
            app.Dependencies.Clear();
            foreach (var dependency in reference.Dependencies)
            {
                app.Dependencies.Add(new LibraryDependency { Name = dependency.Name, Version = dependency.Version });
            }

            await Storage.WriteLibraryIndexJsonAsync(reference.Id, index, token).ConfigureAwait(false);
        }

        public async Task UpdatePackageReadMeAsync(Package package, CancellationToken token)
        {
            package.AssertNotNull(nameof(package));

            var id = new LibraryId(package.SourceCode, package.Name, package.Version);
            await Storage.CreateDefaultRemarksFile(id, token).ConfigureAwait(false);
            await Storage.CreateDefaultThirdPartyNoticesFile(id, token).ConfigureAwait(false);

            var index = await Storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(id, token).ConfigureAwait(false);

            var context = new LibraryReadMeContext
            {
                Name = package.Name,
                Version = package.Version,
                HRef = package.HRef,
                Description = package.Description,
                LicenseCode = package.LicenseCode,
                UsedBy = PackageRepositoryTools.BuildUsedBy(package.UsedBy),
                TargetFrameworks = string.Join(", ", index.UsedBy.SelectMany(i => i.TargetFrameworks).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(i => i)),
                Remarks = package.Remarks,
                ThirdPartyNotices = package.ThirdPartyNotices
            };

            if (context.LicenseCode.IsNullOrEmpty())
            {
                context.LicenseCode = "Unknown";
            }
            else
            {
                var codes = LicenseExpression.GetCodes(context.LicenseCode);

                context.LicenseLocalHRef = Storage.GetLicenseLocalHRef(codes.First(), id);
                context.LicenseMarkdownExpression = LicenseExpression.ReplaceCodes(
                    context.LicenseCode,
                    i => "[{0}]({1})".FormatWith(i, Storage.GetLicenseLocalHRef(i, id)));

                if (package.ApprovalStatus == PackageApprovalStatus.HasToBeApproved)
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
                .Select(i => new LibraryId(id.SourceCode, i.Name, i.Version))
                .Distinct();
            foreach (var dependency in dependencies)
            {
                context.Dependencies.Add(new LibraryReadMeDependencyContext
                {
                    Name = dependency.Name,
                    Version = dependency.Version,
                    LocalHRef = Storage.GetPackageLocalHRef(dependency, id)
                });
            }

            await Storage.WriteLibraryReadMeAsync(id, context, token).ConfigureAwait(false);
        }

        public async ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token)
        {
            appName.AssertNotNull(nameof(appName));

            var model = await Storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(id, token).ConfigureAwait(false);
            var result = PackageRemoveResult.None;

            var index = model.UsedBy.IndexOf(i => i.Name.EqualsIgnoreCase(appName));
            if (index >= 0)
            {
                model.UsedBy.RemoveAt(index);
                await Storage.WriteLibraryIndexJsonAsync(id, model, token).ConfigureAwait(false);

                result = model.UsedBy.Count == 0 ? PackageRemoveResult.RemovedNoRefs : PackageRemoveResult.Removed;
            }

            return result;
        }

        protected abstract Task AppendSpecAttributesAsync(LibraryId id, Package package, CancellationToken token);
    }
}
