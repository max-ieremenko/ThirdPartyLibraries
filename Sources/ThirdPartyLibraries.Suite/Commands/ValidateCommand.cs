using System;
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
    public sealed class ValidateCommand : ICommand
    {
        public ValidateCommand(IUnityContainer container, ILogger logger)
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

        public async ValueTask<bool> ExecuteAsync(CancellationToken token)
        {
            var state = new ValidateCommandState(Container.Resolve<IPackageRepository>(), AppName);
            await state.InitializeAsync(token);

            var issues = GetIssues(state)
                .GroupBy(i => i.Issue, i => i.Id, StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i.Key);

            var hasIssues = false;
            foreach (var issue in issues)
            {
                hasIssues = true;

                Logger.Error("Following libraries {0}:".FormatWith(issue.Key));
                using (Logger.Indent())
                {
                    foreach (var id in issue)
                    {
                        Logger.Error("{0} {1} from {2}".FormatWith(id.Name, id.Version, id.SourceCode));
                    }
                }
            }

            return !hasIssues;
        }

        private IEnumerable<(LibraryId Id, string Issue)> GetIssues(ValidateCommandState state)
        {
            var references = Container.Resolve<ISourceCodeParser>().GetReferences(Sources);

            var messageNotAssigned = "are not assigned to {0}".FormatWith(AppName);
            var messageReferencesNotFound = "are assigned to {0}, but references not found in the sources".FormatWith(AppName);

            foreach (var reference in references)
            {
                if (!state.PackageExists(reference.Id))
                {
                    yield return (reference.Id, "not found in the repository");
                    continue;
                }

                if (!state.IsAssignedToApp(reference.Id))
                {
                    yield return (reference.Id, messageNotAssigned);
                }

                if (state.GetPackageLicenseCode(reference.Id).IsNullOrEmpty())
                {
                    yield return (reference.Id, "have no license");
                    continue;
                }

                var approvalStatus = state.GetPackageApprovalStatus(reference.Id);
                var requiresApproval = state.GetLicenseRequiresApproval(reference.Id);

                if (approvalStatus == PackageApprovalStatus.HasToBeApproved
                    || (requiresApproval && approvalStatus == PackageApprovalStatus.AutomaticallyApproved))
                {
                    yield return (reference.Id, "are not approved");
                }

                if (state.GetLicenseRequiresThirdPartyNotices(reference.Id) && !state.GetPackageHasThirdPartyNotices(reference.Id))
                {
                    yield return (reference.Id, "have no third party notices");
                }
            }

            foreach (var id in state.GetAppTrash())
            {
                yield return (id, messageReferencesNotFound);
            }
        }
    }
}
