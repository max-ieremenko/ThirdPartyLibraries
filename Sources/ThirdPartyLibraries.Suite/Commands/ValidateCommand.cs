using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands;

public sealed class ValidateCommand : ICommand
{
    public string AppName { get; set; }

    public IList<string> Sources { get; } = new List<string>();

    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        var repository = serviceProvider.GetRequiredService<IPackageRepository>();

        Hello(serviceProvider.GetRequiredService<ILogger>(), repository);

        var state = new ValidateCommandState(repository, AppName);
        await state.InitializeAsync(token).ConfigureAwait(false);

        var issues = GetIssues(serviceProvider.GetRequiredService<ISourceCodeParser>(), state)
            .GroupBy(i => i.Issue, i => i.Id, StringComparer.OrdinalIgnoreCase)
            .OrderBy(i => i.Key);

        var errors = new List<RepositoryValidationError>();

        foreach (var issue in issues)
        {
            errors.Add(new RepositoryValidationError(issue.Key, issue.ToArray()));
        }

        if (errors.Count > 0)
        {
            throw new RepositoryValidationException(errors.ToArray());
        }
    }

    private void Hello(ILogger logger, IPackageRepository repository)
    {
        logger.Info("validate application {0}".FormatWith(AppName));
        using (logger.Indent())
        {
            logger.Info("repository {0}".FormatWith(repository.Storage.ConnectionString));
            logger.Info("sources " + string.Join(", ", Sources));
        }
    }

    private IEnumerable<(LibraryId Id, string Issue)> GetIssues(ISourceCodeParser parser, ValidateCommandState state)
    {
        var references = parser.GetReferences(Sources);

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