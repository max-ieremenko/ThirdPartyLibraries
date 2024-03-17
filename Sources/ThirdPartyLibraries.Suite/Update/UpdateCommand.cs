using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Remove;
using ThirdPartyLibraries.Suite.Shared;
using ThirdPartyLibraries.Suite.Update.Internal;

namespace ThirdPartyLibraries.Suite.Update;

public sealed class UpdateCommand : ICommand
{
    public string AppName { get; set; } = null!;

    public List<string> Sources { get; } = new();

    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        var logger = serviceProvider.GetRequiredService<ILogger>();

        Hello(
            logger,
            serviceProvider.GetRequiredService<IStorage>().ConnectionString);

        var updateResult = await UpdateReferencesAsync(
                logger,
                serviceProvider.GetRequiredService<ISourceCodeParser>(),
                serviceProvider.GetRequiredService<IPackageContentUpdater>(),
                serviceProvider.GetRequiredService<IPackageLicenseUpdater>(),
                token)
            .ConfigureAwait(false);

        await UpdateCustomPackagesAsync(
                logger,
                serviceProvider.GetRequiredService<ICustomPackageUpdater>(),
                token)
            .ConfigureAwait(false);

        var removeResult = await RemoveFromApplicationAsync(
                logger,
                serviceProvider.GetRequiredService<IPackageRemover>(),
                updateResult.Ids,
                token)
            .ConfigureAwait(false);

        logger.Info($"New {updateResult.Created}; updated {updateResult.Updated + removeResult.Updated}; removed {removeResult.Deleted}; unchanged {updateResult.Unchanged}");
    }

    private void Hello(ILogger logger, string storageConnectionString)
    {
        logger.Info($"update application {AppName}");
        using (logger.Indent())
        {
            logger.Info($"repository {storageConnectionString}");
            logger.Info("sources " + string.Join(", ", Sources));
        }
    }

    private async Task<(HashSet<LibraryId> Ids, int Created, int Updated, int Unchanged)> UpdateReferencesAsync(
        ILogger logger,
        ISourceCodeParser sourceCodeParser,
        IPackageContentUpdater contentUpdater,
        IPackageLicenseUpdater licenseUpdater,
        CancellationToken token)
    {
        var references = sourceCodeParser.GetReferences(Sources);
        var ids = new HashSet<LibraryId>(references.Count);

        var orderedReferences = sourceCodeParser.GetReferences(Sources);

        var createdCount = 0;
        var updatedCount = 0;
        var unchangedCount = 0;

        foreach (var reference in orderedReferences)
        {
            logger.Info($"Validate reference {reference.Id.Name} {reference.Id.Version} from {reference.Id.SourceCode}");
            ids.Add(reference.Id);

            var contentResult = await contentUpdater.UpdateAsync(reference, AppName, token).ConfigureAwait(false);
            var licenseResult = await licenseUpdater.UpdateAsync(reference.Id, token).ConfigureAwait(false);

            if (contentResult == UpdateResult.Created)
            {
                createdCount++;
            }
            else if (contentResult == UpdateResult.Updated || licenseResult)
            {
                updatedCount++;
            }
            else
            {
                unchangedCount++;
            }
        }

        return (ids, createdCount, updatedCount, unchangedCount);
    }

    private async Task UpdateCustomPackagesAsync(ILogger logger, ICustomPackageUpdater updater, CancellationToken token)
    {
        var orderedAllIds = await updater.GetAllCustomLibrariesAsync(token).ConfigureAwait(false);

        foreach (var id in orderedAllIds)
        {
            logger.Info($"Update {id.Name} {id.Version} from {id.SourceCode}");
            await updater.UpdateAsync(id, token).ConfigureAwait(false);
        }
    }

    private async Task<(int Updated, int Deleted)> RemoveFromApplicationAsync(
        ILogger logger,
        IPackageRemover remover,
        HashSet<LibraryId> references,
        CancellationToken token)
    {
        var orderedAllIds = await remover.GetAllLibrariesAsync(token).ConfigureAwait(false);

        var updatedCount = 0;
        var deletedCount = 0;

        foreach (var id in orderedAllIds)
        {
            if (references.Contains(id))
            {
                continue;
            }

            var result = await remover.RemoveFromApplicationAsync(id, AppName, token).ConfigureAwait(false);
            if (result == RemoveResult.Deleted)
            {
                deletedCount++;
                logger.Info($"The {id.SourceCode} {id.Name} {id.Version} has been completely removed from the repository");
            }
            else if (result == RemoveResult.Updated)
            {
                updatedCount++;
                logger.Info($"The reference to {id.SourceCode} {id.Name} {id.Version} has been removed from the application");
            }
        }

        return (updatedCount, deletedCount);
    }
}