﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands;

public sealed class UpdateCommand : ICommand
{
    public string AppName { get; set; }

    public IList<string> Sources { get; } = new List<string>();
        
    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        var repository = serviceProvider.GetRequiredService<IPackageRepository>();
        var logger = serviceProvider.GetRequiredService<ILogger>();

        Hello(logger, repository);

        var state = new UpdateCommandState(repository);

        var (created, updated) = await UpdateReferencesAsync(state, serviceProvider, logger, token).ConfigureAwait(false);
        var (softRemoved, hardRemoved) = await RemoveFromApplicationAsync(state, logger, token).ConfigureAwait(false);

        logger.Info("New {0}; updated {1}; removed {2}".FormatWith(created, updated, softRemoved + hardRemoved));
    }

    private void Hello(ILogger logger, IPackageRepository repository)
    {
        logger.Info("update application {0}".FormatWith(AppName));
        using (logger.Indent())
        {
            logger.Info("repository {0}".FormatWith(repository.Storage.ConnectionString));
            logger.Info("sources " + string.Join(", ", Sources));
        }
    }

    private async Task<(int SoftCount, int HardCount)> RemoveFromApplicationAsync(UpdateCommandState state, ILogger logger, CancellationToken token)
    {
        var toRemove = await state.GetIdsToRemoveAsync(token).ConfigureAwait(false);
        var order = toRemove
            .OrderBy(i => i.SourceCode)
            .ThenBy(i => i.Name)
            .ThenBy(i => i.Version);

        var softCount = 0;
        var hardCount = 0;

        foreach (var id in order)
        {
            var action = await state.Repository.RemoveFromApplicationAsync(id, AppName, token).ConfigureAwait(false);
            if (action == PackageRemoveResult.Removed)
            {
                softCount++;
                logger.Info("Reference {0} {1} {2} was removed from application {3}".FormatWith(id.SourceCode, id.Name, id.Version, AppName));
            }
            else if (action == PackageRemoveResult.RemovedNoRefs)
            {
                hardCount++;
                logger.Info("Reference {0} {1} {2} was completely removed from repository".FormatWith(id.SourceCode, id.Name, id.Version));
            }
        }

        return (softCount, hardCount);
    }

    private async Task<(int NewCount, int UpdatedCount)> UpdateReferencesAsync(
        UpdateCommandState state,
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken token)
    {
        var codeParser = serviceProvider.GetRequiredService<ISourceCodeParser>();

        var references = codeParser
            .GetReferences(Sources)
            .OrderBy(i => i.Id.SourceCode)
            .ThenBy(i => i.Id.Name)
            .ThenBy(i => i.Id.Version);

        var newCount = 0;
        var updatedCount = 0;

        foreach (var reference in references)
        {
            logger.Info("Validate reference {0} {1} from {2}".FormatWith(reference.Id.Name, reference.Id.Version, reference.Id.SourceCode));
            using (logger.Indent())
            {
                var isNew = await serviceProvider.GetRequiredKeyedService<IPackageResolver>(reference.Id.SourceCode).DownloadAsync(reference.Id, token).ConfigureAwait(false);
                var package = await state.LoadPackageAsync(reference.Id, token).ConfigureAwait(false);

                if (isNew)
                {
                    newCount++;
                }
                else
                {
                    updatedCount++;
                }

                await ValidatePackageAsync(state, package, logger, token).ConfigureAwait(false);
                await state.UpdatePackageAsync(reference, package, AppName, token).ConfigureAwait(false);
            }
        }

        return (newCount, updatedCount);
    }

    private async Task ValidatePackageAsync(UpdateCommandState state, Package package, ILogger logger, CancellationToken token)
    {
        foreach (var license in package.Licenses)
        {
            if (license.Code.IsNullOrEmpty())
            {
                logger.Info("{0} license cannot be resolved automatically: {1}".FormatWith(license.Subject, license.CodeDescription));
            }
            else
            {
                await state.LicenseRequiresApprovalAsync(license.Code, token).ConfigureAwait(false);
                logger.Info("{0} license: {1}".FormatWith(license.Subject, license.Code));
            }
        }

        if (package.ApprovalStatus == PackageApprovalStatus.None)
        {
            package.ApprovalStatus = PackageApprovalStatus.HasToBeApproved;
        }

        if (package.LicenseCode.IsNullOrEmpty())
        {
            if (package.ApprovalStatus != PackageApprovalStatus.HasToBeApproved)
            {
                logger.Info("License cannot be resolved automatically.");
                package.ApprovalStatus = PackageApprovalStatus.HasToBeApproved;
            }
        }
        else
        {
            var licenseRequiresApproval = await state.LicenseRequiresApprovalAsync(package.LicenseCode, token).ConfigureAwait(false);
            if (licenseRequiresApproval)
            {
                if (package.ApprovalStatus == PackageApprovalStatus.AutomaticallyApproved)
                {
                    logger.Info("License {0} (has to be approved)".FormatWith(package.LicenseCode));
                    package.ApprovalStatus = PackageApprovalStatus.HasToBeApproved;
                }
            }
            else if (package.ApprovalStatus == PackageApprovalStatus.HasToBeApproved)
            {
                logger.Info("License: {0} (auto-approve)".FormatWith(package.LicenseCode));
                package.ApprovalStatus = PackageApprovalStatus.AutomaticallyApproved;
            }
        }
    }
}