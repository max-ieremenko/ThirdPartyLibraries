using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Remove;

public sealed class RemoveCommand : ICommand
{
    public List<string> AppNames { get; } = new();

    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        var logger = serviceProvider.GetRequiredService<ILogger>();

        Hello(
            logger,
            serviceProvider.GetRequiredService<IStorage>().ConnectionString);

        await RemoveFromApplicationsAsync(
                logger,
                serviceProvider.GetRequiredService<IPackageRemover>(),
                token)
            .ConfigureAwait(false);
    }

    private async Task RemoveFromApplicationsAsync(
        ILogger logger,
        IPackageRemover remover,
        CancellationToken token)
    {
        var orderedAllIds = await remover.GetAllLibrariesAsync(token).ConfigureAwait(false);

        var updated = new HashSet<LibraryId>();
        var deleted = new HashSet<LibraryId>();

        foreach (var id in orderedAllIds)
        {
            foreach (var appName in AppNames)
            {
                var result = await remover.RemoveFromApplicationAsync(id, appName, token).ConfigureAwait(false);
                if (result == RemoveResult.Deleted)
                {
                    deleted.Add(id);
                    logger.Info($"The {id.SourceCode} {id.Name} {id.Version} has been completely removed from the repository");
                }
                else if (result == RemoveResult.Updated)
                {
                    updated.Add(id);
                    logger.Info($"The reference to {id.SourceCode} {id.Name} {id.Version} has been removed from the application");
                }
            }
        }
        
        updated.ExceptWith(deleted);

        var unchangedCount = orderedAllIds.Count - updated.Count - deleted.Count;

        logger.Info($"Updated {updated.Count}; removed {deleted.Count}; unchanged {unchangedCount}");
    }

    private void Hello(ILogger logger, string storageConnectionString)
    {
        var appNames = string.Join(", ", AppNames);
        logger.Info($"remove {appNames} from repository");
        using (logger.Indent())
        {
            logger.Info($"repository {storageConnectionString}");
        }
    }
}