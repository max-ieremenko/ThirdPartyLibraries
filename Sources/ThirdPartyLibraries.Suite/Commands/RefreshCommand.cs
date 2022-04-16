using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;

namespace ThirdPartyLibraries.Suite.Commands
{
    public sealed class RefreshCommand : ICommand
    {
        public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            var repository = serviceProvider.GetRequiredService<IPackageRepository>();
            var state = new RefreshCommandState(repository);
            var packages = await repository.UpdateAllPackagesReadMeAsync(token).ConfigureAwait(false);

            var rootContext = new RootReadMeContext();

            foreach (var metadata in packages.OrderBy(i => i.Name).ThenBy(i => i.Version).ThenBy(i => i.SourceCode))
            {
                var (licenses, markdownExpression) = await state.GetLicensesAsync(metadata.LicenseCode, token).ConfigureAwait(false);

                foreach (var license in licenses)
                {
                    license.PackagesCount++;
                }

                var packageContext = new RootReadMePackageContext
                {
                    Source = metadata.SourceCode,
                    Name = metadata.Name,
                    Version = metadata.Version,
                    License = metadata.LicenseCode,
                    IsApproved = metadata.ApprovalStatus == PackageApprovalStatus.Approved || metadata.ApprovalStatus == PackageApprovalStatus.AutomaticallyApproved,
                    UsedBy = PackageRepositoryTools.BuildUsedBy(metadata.UsedBy),
                    SourceHRef = metadata.HRef,
                    LocalHRef = repository.Storage.GetPackageLocalHRef(new LibraryId(metadata.SourceCode, metadata.Name, metadata.Version)),
                    LicenseLocalHRef = licenses.FirstOrDefault()?.LocalHRef,
                    LicenseMarkdownExpression = markdownExpression
                };

                if (packageContext.SourceHRef.IsNullOrEmpty())
                {
                    packageContext.SourceHRef = packageContext.LocalHRef;
                }

                rootContext.Packages.Add(packageContext);
            }

            rootContext.Licenses.AddRange(state.Licenses.OrderBy(i => i.Code));

            rootContext.TodoPackages.AddRange(rootContext.Packages.Where(i => !i.IsApproved || i.License.IsNullOrEmpty()));

            await repository.Storage.WriteRootReadMeAsync(rootContext, token).ConfigureAwait(false);
        }
    }
}
