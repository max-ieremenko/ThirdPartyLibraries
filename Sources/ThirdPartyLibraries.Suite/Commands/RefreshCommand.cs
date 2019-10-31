using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;
using Unity;

namespace ThirdPartyLibraries.Suite.Commands
{
    public sealed class RefreshCommand : ICommand
    {
        public RefreshCommand(IUnityContainer container, ILogger logger)
        {
            Container = container;
            Logger = logger;
        }

        public IUnityContainer Container { get; }

        public ILogger Logger { get; }
     
        public async ValueTask<bool> ExecuteAsync(CancellationToken token)
        {
            var repository = Container.Resolve<IPackageRepository>();
            var state = new RefreshCommandState(repository);
            var packages = await repository.UpdateAllPackagesReadMeAsync(token);

            var rootContext = new RootReadMeContext();

            foreach (var metadata in packages.OrderBy(i => i.Name).ThenBy(i => i.Version).ThenBy(i => i.SourceCode))
            {
                IList<RootReadMeLicenseContext> licenses = Array.Empty<RootReadMeLicenseContext>();
                if (!metadata.LicenseCode.IsNullOrEmpty())
                {
                    licenses = await state.GetLicensesAsync(metadata.LicenseCode, token);
                }

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
                    ApprovalStatus = "TODO",
                    UsedBy = metadata.UsedBy,
                    SourceHRef = metadata.HRef,
                    LocalHRef = repository.Storage.GetPackageLocalHRef(new LibraryId(metadata.SourceCode, metadata.Name, metadata.Version), RelativeTo.Root),
                    LicenseLocalHRef = licenses.FirstOrDefault()?.LocalHRef
                };

                if (packageContext.SourceHRef.IsNullOrEmpty())
                {
                    packageContext.SourceHRef = packageContext.LocalHRef;
                }

                if (packageContext.IsApproved)
                {
                    packageContext.ApprovalStatus = metadata.ApprovalStatus == PackageApprovalStatus.Approved ? "OK" : "auto";
                }

                rootContext.Packages.Add(packageContext);
            }

            rootContext.Licenses.AddRange(state.Licenses.OrderBy(i => i.Code));

            rootContext.TodoPackages.AddRange(rootContext.Packages.Where(i => !i.IsApproved || i.License.IsNullOrEmpty()));

            await repository.Storage.WriteRootReadMeAsync(rootContext, token);

            return true;
        }
    }
}
