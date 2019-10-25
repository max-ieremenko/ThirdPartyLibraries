using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;
using Unity;

namespace ThirdPartyLibraries.Suite.Commands
{
    public sealed class UpdateCommand : ICommand
    {
        public UpdateCommand(IUnityContainer container, ILogger logger)
        {
            container.AssertNotNull(nameof(container));
            logger.AssertNotNull(nameof(logger));

            Container = container;
            Logger = logger;
        }

        public IUnityContainer Container { get; }

        public ILogger Logger { get; }

        public string AppName { get; set; }

        public IList<string> Sources { get; } = new List<string>();
        
        public async Task ExecuteAsync(CancellationToken token)
        {
            var repository = Container.Resolve<IPackageRepository>();
            var state = new UpdateCommandState(repository);

            var (created, updated) = await UpdateReferencesAsync(state, token);
            var (softRemoved, hardRemoved) = await RemoveFromApplicationAsync(state, token);

            Logger.Info("New {0}; updated {1}; removed {2}".FormatWith(created, updated, softRemoved + hardRemoved));
        }

        private async Task<(int SoftCount, int HardCount)> RemoveFromApplicationAsync(UpdateCommandState state, CancellationToken token)
        {
            var toRemove = await state.GetIdsToRemoveAsync(token);
            var order = toRemove
                .OrderBy(i => i.SourceCode)
                .ThenBy(i => i.Name)
                .ThenBy(i => i.Version);

            var softCount = 0;
            var hardCount = 0;

            foreach (var id in order)
            {
                var action = await state.Repository.RemoveFromApplicationAsync(id, AppName, token);
                if (action == PackageRemoveResult.Removed)
                {
                    softCount++;
                    Logger.Info("Reference {0} {1} {2} was removed from application {3}".FormatWith(id.SourceCode, id.Name, id.Version, AppName));
                }
                else if (action == PackageRemoveResult.RemovedNoRefs)
                {
                    hardCount++;
                    Logger.Info("Reference {0} {1} {2} was completely removed from repository".FormatWith(id.SourceCode, id.Name, id.Version));
                }
            }

            return (softCount, hardCount);
        }

        private async Task<(int NewCount, int UpdatedCount)> UpdateReferencesAsync(UpdateCommandState state, CancellationToken token)
        {
            var codeParser = Container.Resolve<ISourceCodeParser>();

            var references = codeParser
                .GetReferences(Sources)
                .OrderBy(i => i.Id.SourceCode)
                .ThenBy(i => i.Id.Name)
                .ThenBy(i => i.Id.Version);

            var newCount = 0;
            var updatedCount = 0;

            foreach (var reference in references)
            {
                var package = await state.LoadPackageAsync(reference.Id, token);
                if (package == null)
                {
                    newCount++;
                    package = await ResolveNewPackageAsync(state, reference.Id, token);
                }
                else
                {
                    updatedCount++;
                    await ValidateExistingPackageAsync(state, package, token);
                }

                await state.UpdatePackageAsync(reference, package, AppName, token);
            }

            return (newCount, updatedCount);
        }

        private async Task<Package> ResolveNewPackageAsync(UpdateCommandState state, LibraryId id, CancellationToken token)
        {
            Logger.Info("Resolve new reference {0} {1} from {2}".FormatWith(id.Name, id.Version, id.SourceCode));

            var resolver = Container.Resolve<IPackageResolver>(id.SourceCode);
            var package = await resolver.DownloadAsync(id, token);

            using (Logger.Indent())
            {
                if (package.LicenseCode.IsNullOrEmpty())
                {
                    Logger.Info("License cannot be resolved automatically.");
                    package.ApprovalStatus = PackageApprovalStatus.HasToBeApproved;
                }
                else
                {
                    var licenseRequiresApproval = await state.LicenseRequiresApprovalAsync(package.LicenseCode, token);
                    if (licenseRequiresApproval)
                    {
                        Logger.Info("License {0} (has to be approved)".FormatWith(package.LicenseCode));
                        package.ApprovalStatus = PackageApprovalStatus.HasToBeApproved;
                    }
                    else
                    {
                        Logger.Info("License {0} (auto-approve)".FormatWith(package.LicenseCode));
                        package.ApprovalStatus = PackageApprovalStatus.AutomaticallyApproved;
                    }
                }

                foreach (var row in package.Licenses)
                {
                    if (row.Code.IsNullOrEmpty())
                    {
                        Logger.Info("{0} license cannot be resolved automatically: {1}".FormatWith(row.Subject, row.CodeDescription));
                    }
                    else
                    {
                        await state.LicenseRequiresApprovalAsync(row.Code, token);
                        Logger.Info("{0} license: {1}".FormatWith(row.Subject, row.Code));
                    }
                }
            }

            return package;
        }

        private async Task ValidateExistingPackageAsync(UpdateCommandState state, Package package, CancellationToken token)
        {
            Logger.Info("Validate existing reference {0} {1} from {2}".FormatWith(package.Name, package.Version, package.SourceCode));

            using (Logger.Indent())
            {
                if (package.LicenseCode.IsNullOrEmpty())
                {
                    Logger.Info("Update license.");
                    var resolver = Container.Resolve<IPackageResolver>(package.SourceCode);
                    await resolver.TryUpdateLicenseAsync(package, token);
                }

                if (package.LicenseCode.IsNullOrEmpty())
                {
                    if (package.ApprovalStatus != PackageApprovalStatus.HasToBeApproved)
                    {
                        Logger.Info("License cannot be resolved automatically.");
                        package.ApprovalStatus = PackageApprovalStatus.HasToBeApproved;
                    }
                }
                else
                {
                    var licenseRequiresApproval = await state.LicenseRequiresApprovalAsync(package.LicenseCode, token);
                    if (licenseRequiresApproval)
                    {
                        if (package.ApprovalStatus == PackageApprovalStatus.AutomaticallyApproved)
                        {
                            Logger.Info("License {0} (has to be approved)".FormatWith(package.LicenseCode));
                            package.ApprovalStatus = PackageApprovalStatus.HasToBeApproved;
                        }
                    }
                    else if (package.ApprovalStatus == PackageApprovalStatus.HasToBeApproved)
                    {
                        Logger.Info("License: {0} (auto-approve)".FormatWith(package.LicenseCode));
                        package.ApprovalStatus = PackageApprovalStatus.AutomaticallyApproved;
                    }
                }
            }
        }
    }
}
