using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Shared;
using ThirdPartyLibraries.Suite.Validate.Internal;

namespace ThirdPartyLibraries.Suite.Validate;

public sealed class ValidateCommand : ICommand
{
    public string AppName { get; set; } = null!;

    public List<string> Sources { get; } = new();

    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        Hello(
            serviceProvider.GetRequiredService<ILogger>(),
            serviceProvider.GetRequiredService<IStorage>().ConnectionString);

        var state = serviceProvider.GetRequiredService<IValidationState>();
        await state.InitializeAsync(token).ConfigureAwait(false);

        await ValidateAsync(
                serviceProvider.GetRequiredService<ISourceCodeParser>(),
                state,
                serviceProvider.GetRequiredService<IPackageValidator>(),
                token)
            .ConfigureAwait(false);

        var errors = GetErrors(state);
        if (errors.Count > 0)
        {
            throw new RepositoryValidationException(errors.ToArray());
        }
    }

    private void Hello(ILogger logger, string storageConnectionString)
    {
        logger.Info($"validate application {AppName}");
        using (logger.Indent())
        {
            logger.Info($"repository {storageConnectionString}");
            logger.Info("sources " + string.Join(", ", Sources));
        }
    }

    private async Task ValidateAsync(ISourceCodeParser sourceCodeParser, IValidationState state, IPackageValidator validator, CancellationToken token)
    {
        var references = sourceCodeParser.GetReferences(Sources);

        for (var i = 0; i < references.Count; i++)
        {
            var reference = references[i];
            var result = await validator.ValidateReferenceAsync(reference, AppName, token).ConfigureAwait(false);
            state.SetResult(reference.Id, result);
        }

        var rest = state.GetNotProcessed();
        for (var i = 0; i < rest.Count; i++)
        {
            var id = rest[i];
            var result = await validator.ValidateLibraryAsync(id, AppName, token).ConfigureAwait(false);
            state.SetResult(id, result);
        }
    }

    private List<RepositoryValidationError> GetErrors(IValidationState state)
    {
        var result = new List<RepositoryValidationError>();

        foreach (ValidationResult issue in Enum.GetValues(typeof(ValidationResult)))
        {
            if (issue == ValidationResult.Success)
            {
                continue;
            }

            var libraries = state.GetWithError(issue);
            if (libraries != null)
            {
                result.Add(new RepositoryValidationError(issue, AppName, libraries));
            }
        }

        return result;
    }
}