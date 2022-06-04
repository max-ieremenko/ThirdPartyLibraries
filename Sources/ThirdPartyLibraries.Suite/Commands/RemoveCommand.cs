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

public sealed class RemoveCommand : ICommand
{
    public IList<string> AppNames { get; } = new List<string>();

    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        var repository = serviceProvider.GetRequiredService<IPackageRepository>();
        var logger = serviceProvider.GetRequiredService<ILogger>();

        Hello(logger, repository);

        var libraries = await repository.Storage.GetAllLibrariesAsync(token).ConfigureAwait(false);
        var order = libraries
            .OrderBy(i => i.SourceCode)
            .ThenBy(i => i.Name)
            .ThenBy(i => i.Version);

        var updatedLibraries = new HashSet<LibraryId>();
        var removedLibraries = new HashSet<LibraryId>();

        foreach (var library in order)
        {
            foreach (var appName in AppNames)
            {
                var action = await repository.RemoveFromApplicationAsync(library, appName, token).ConfigureAwait(false);
                if (action == PackageRemoveResult.None)
                {
                    continue;
                }

                logger.Info("{0} reference was removed from {1} {2} {3}".FormatWith(appName, library.SourceCode, library.Name, library.Version));
                if (action == PackageRemoveResult.Removed)
                {
                    updatedLibraries.Add(library);
                }
                else if (action == PackageRemoveResult.RemovedNoRefs)
                {
                    removedLibraries.Add(library);
                    logger.Info("{0} {1} {2} was completely removed from repository".FormatWith(library.SourceCode, library.Name, library.Version));
                }
            }
        }

        updatedLibraries.ExceptWith(removedLibraries);

        logger.Info("Updated {0}; removed {1}".FormatWith(updatedLibraries.Count, removedLibraries.Count));
    }

    private void Hello(ILogger logger, IPackageRepository repository)
    {
        logger.Info("remove {0} from repository".FormatWith(string.Join(", ", AppNames)));
        using (logger.Indent())
        {
            logger.Info("repository {0}".FormatWith(repository.Storage.ConnectionString));
        }
    }
}