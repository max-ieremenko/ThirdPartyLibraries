using System;
using System.Collections.Generic;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite;
using ThirdPartyLibraries.Suite.Generate;
using ThirdPartyLibraries.Suite.Refresh;
using ThirdPartyLibraries.Suite.Remove;
using ThirdPartyLibraries.Suite.Update;
using ThirdPartyLibraries.Suite.Validate;

namespace ThirdPartyLibraries;

internal static class CommandFactory
{
    public static ICommand Create(CommandLine line, Dictionary<string, string?> configuration, out string? repository)
    {
        repository = null;
        if (string.IsNullOrEmpty(line.Command))
        {
            return CreateHelp(null);
        }

        if (CommandOptions.CommandUpdate.Equals(line.Command, StringComparison.OrdinalIgnoreCase))
        {
            if (IsHelp(line.Options))
            {
                return CreateHelp(CommandOptions.CommandUpdate);
            }

            var update = CreateUpdateCommand(line.Options, configuration, out repository);
            return new CommandChain(update, new RefreshCommand());
        }
            
        if (CommandOptions.CommandRefresh.Equals(line.Command, StringComparison.OrdinalIgnoreCase))
        {
            if (IsHelp(line.Options))
            {
                return CreateHelp(CommandOptions.CommandRefresh);
            }

            return CreateRefreshCommand(line.Options, out repository);
        }
            
        if (CommandOptions.CommandValidate.Equals(line.Command, StringComparison.OrdinalIgnoreCase))
        {
            if (IsHelp(line.Options))
            {
                return CreateHelp(CommandOptions.CommandValidate);
            }

            return CreateValidateCommand(line.Options, out repository);
        }
            
        if (CommandOptions.CommandGenerate.Equals(line.Command, StringComparison.OrdinalIgnoreCase))
        {
            if (IsHelp(line.Options))
            {
                return CreateHelp(CommandOptions.CommandGenerate);
            }

            return CreateGenerateCommand(line.Options, out repository);
        }

        if (CommandOptions.CommandRemove.Equals(line.Command, StringComparison.OrdinalIgnoreCase))
        {
            if (IsHelp(line.Options))
            {
                return CreateHelp(CommandOptions.CommandRemove);
            }

            var remove = CreateRemoveCommand(line.Options, out repository);
            return new CommandChain(remove, new RefreshCommand());
        }

        throw new InvalidOperationException($"Unknown command [{line.Command}].");
    }

    private static bool IsHelp(IList<CommandOption> options)
    {
        return options.Count == 0 || (options.Count == 1 && CommandOptions.OptionHelp.Equals(options[0].Name));
    }

    private static ICommand CreateHelp(string? command) => new HelpCommand(command);

    private static UpdateCommand CreateUpdateCommand(IList<CommandOption> options, Dictionary<string, string?> configuration, out string? repository)
    {
        var result = new UpdateCommand();
        repository = null;

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];

            if (option.IsSource(out var value))
            {
                result.Sources.Add(value);
            }
            else if (option.IsAppName(out value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionAppName, !result.AppName.IsNullOrEmpty());
                result.AppName = value;
            }
            else if (option.IsRepository(out value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionRepository, !repository.IsNullOrEmpty());
                repository = value;
            }
            else if (option.IsGitHubToken(out value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionGitHubToken, configuration.ContainsKey(CommandOptions.OptionGitHubToken));
                configuration.Add(CommandOptions.OptionGitHubToken, value);
            }
            else
            {
                CommandOptions.AssertUnknown(option.Name);
            }
        }

        CommandOptions.AssertMissing(CommandOptions.OptionAppName, result.AppName.IsNullOrEmpty());
        CommandOptions.AssertMissing(CommandOptions.OptionSource, result.Sources.Count == 0);
        CommandOptions.AssertMissing(CommandOptions.OptionRepository, repository.IsNullOrEmpty());

        return result;
    }

    private static RefreshCommand CreateRefreshCommand(IList<CommandOption> options, out string repository)
    {
        var result = new RefreshCommand();
        repository = null!;

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];

            if (option.IsRepository(out var value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionRepository, !repository.IsNullOrEmpty());
                repository = value;
            }
            else
            {
                CommandOptions.AssertUnknown(option.Name);
            }
        }

        CommandOptions.AssertMissing(CommandOptions.OptionRepository, repository.IsNullOrEmpty());

        return result;
    }

    private static ValidateCommand CreateValidateCommand(IList<CommandOption> options, out string repository)
    {
        var result = new ValidateCommand();
        repository = null!;

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];

            if (option.IsSource(out var value))
            {
                result.Sources.Add(value);
            }
            else if (option.IsAppName(out value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionAppName, !result.AppName.IsNullOrEmpty());
                result.AppName = value;
            }
            else if (option.IsRepository(out value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionRepository, !repository.IsNullOrEmpty());
                repository = value;
            }
            else
            {
                CommandOptions.AssertUnknown(option.Name);
            }
        }

        CommandOptions.AssertMissing(CommandOptions.OptionAppName, result.AppName.IsNullOrEmpty());
        CommandOptions.AssertMissing(CommandOptions.OptionSource, result.Sources.Count == 0);
        CommandOptions.AssertMissing(CommandOptions.OptionRepository, repository.IsNullOrEmpty());

        return result;
    }

    private static GenerateCommand CreateGenerateCommand(IList<CommandOption> options, out string repository)
    {
        var result = new GenerateCommand();
        repository = null!;

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];

            if (option.IsTo(out var value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionTo, !result.To.IsNullOrEmpty());
                result.To = value;
            }
            else if (option.IsAppName(out value))
            {
                result.AppNames.Add(value);
            }
            else if (option.IsTitle(out value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionTitle, !result.Title.IsNullOrEmpty());
                result.Title = value;
            }
            else if (option.IsRepository(out value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionRepository, !repository.IsNullOrEmpty());
                repository = value;
            }
            else if (option.IsToFileName(out value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionToFileName, !result.ToFileName.IsNullOrEmpty());
                result.ToFileName = value;
            }
            else if (option.IsTemplate(out value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionTemplate, !result.Template.IsNullOrEmpty());
                result.Template = value;
            }
            else
            {
                CommandOptions.AssertUnknown(option.Name);
            }
        }

        CommandOptions.AssertMissing(CommandOptions.OptionAppName, result.AppNames.Count == 0);
        CommandOptions.AssertMissing(CommandOptions.OptionTo, result.To.IsNullOrEmpty());
        CommandOptions.AssertMissing(CommandOptions.OptionRepository, repository.IsNullOrEmpty());

        return result;
    }

    private static RemoveCommand CreateRemoveCommand(IList<CommandOption> options, out string? repository)
    {
        var result = new RemoveCommand();
        repository = null;

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];

            if (option.IsAppName(out var value))
            {
                result.AppNames.Add(value);
            }
            else if (option.IsRepository(out value))
            {
                CommandOptions.AssertDuplicated(CommandOptions.OptionRepository, !string.IsNullOrEmpty(repository));
                repository = value;
            }
            else
            {
                CommandOptions.AssertUnknown(option.Name);
            }
        }

        CommandOptions.AssertMissing(CommandOptions.OptionAppName, result.AppNames.Count == 0);
        CommandOptions.AssertMissing(CommandOptions.OptionRepository, string.IsNullOrEmpty(repository));

        return result;
    }
}