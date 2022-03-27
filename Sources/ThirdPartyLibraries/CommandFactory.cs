using System;
using System.Collections.Generic;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite;
using ThirdPartyLibraries.Suite.Commands;

namespace ThirdPartyLibraries
{
    internal static class CommandFactory
    {
        internal const string CommandUpdate = "update";
        internal const string CommandRefresh = "refresh";
        internal const string CommandValidate = "validate";
        internal const string CommandGenerate = "generate";

        internal const string OptionHelp = "help";
        internal const string OptionAppName = "appName";
        internal const string OptionSource = "source";
        internal const string OptionRepository = "repository";
        internal const string OptionTo = "to";
        internal const string OptionGitHubToken = "github.com:personalAccessToken";

        internal const string UserSecretsId = "c903410c-3d05-49fe-bc8b-b95a2f4dfc69";
        internal const string EnvironmentVariablePrefix = "ThirdPartyLibraries:";

        public static ICommand Create(CommandLine line, out string repository)
        {
            repository = null;
            if (string.IsNullOrEmpty(line.Command))
            {
                return CreateHelp(null);
            }

            if (CommandUpdate.EqualsIgnoreCase(line.Command))
            {
                if (IsHelp(line.Options))
                {
                    return CreateHelp(CommandUpdate);
                }

                var update = CreateUpdateCommand(line.Options, out repository);
                return new CommandChain(update, new RefreshCommand());
            }
            
            if (CommandRefresh.EqualsIgnoreCase(line.Command))
            {
                if (IsHelp(line.Options))
                {
                    return CreateHelp(CommandRefresh);
                }

                return CreateRefreshCommand(line.Options, out repository);
            }
            
            if (CommandValidate.EqualsIgnoreCase(line.Command))
            {
                if (IsHelp(line.Options))
                {
                    return CreateHelp(CommandValidate);
                }

                return CreateValidateCommand(line.Options, out repository);
            }
            
            if (CommandGenerate.EqualsIgnoreCase(line.Command))
            {
                if (IsHelp(line.Options))
                {
                    return CreateHelp(CommandGenerate);
                }

                return CreateGenerateCommand(line.Options, out repository);
            }

            throw new InvalidOperationException("Unknown command [{0}].".FormatWith(line.Command));
        }

        private static bool IsHelp(IList<CommandOption> options)
        {
            return options.Count == 0 || (options.Count == 1 && OptionHelp.Equals(options[0].Name));
        }

        private static ICommand CreateHelp(string command)
        {
            return new HelpCommand(command);
        }

        private static UpdateCommand CreateUpdateCommand(IList<CommandOption> options, out string repository)
        {
            var result = new UpdateCommand();
            repository = null;

            foreach (var option in options)
            {
                if (OptionSource.Equals(option.Name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(option.Value))
                {
                    result.Sources.Add(FileTools.RootPath(option.Value));
                }
                else if (OptionAppName.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(result.AppName))
                    {
                        throw new InvalidOperationException("Option [{0}] is duplicated.".FormatWith(OptionAppName));
                    }

                    result.AppName = option.Value;
                }
                else if (OptionRepository.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!repository.IsNullOrEmpty())
                    {
                        throw new InvalidOperationException("Option [{0}] is duplicated.".FormatWith(OptionRepository));
                    }

                    repository = option.Value;
                }
                else if (OptionGitHubToken.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Environment.SetEnvironmentVariable(EnvironmentVariablePrefix + OptionGitHubToken, option.Value, EnvironmentVariableTarget.Process);
                }
            }

            if (string.IsNullOrEmpty(result.AppName))
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionAppName));
            }

            if (result.Sources.Count == 0)
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionSource));
            }

            if (repository.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionRepository));
            }

            return result;
        }

        private static RefreshCommand CreateRefreshCommand(IList<CommandOption> options, out string repository)
        {
            var result = new RefreshCommand();
            repository = null;

            foreach (var option in options)
            {
                if (OptionRepository.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!repository.IsNullOrEmpty())
                    {
                        throw new InvalidOperationException("Option [{0}] is duplicated.".FormatWith(OptionRepository));
                    }

                    repository = option.Value;
                }
            }

            if (repository.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionRepository));
            }

            return result;
        }

        private static ValidateCommand CreateValidateCommand(IList<CommandOption> options, out string repository)
        {
            var result = new ValidateCommand();
            repository = null;

            foreach (var option in options)
            {
                if (OptionSource.Equals(option.Name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(option.Value))
                {
                    result.Sources.Add(FileTools.RootPath(option.Value));
                }
                else if (OptionAppName.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(result.AppName))
                    {
                        throw new InvalidOperationException("Option [{0}] is duplicated.".FormatWith(OptionAppName));
                    }

                    result.AppName = option.Value;
                }
                else if (OptionRepository.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!repository.IsNullOrEmpty())
                    {
                        throw new InvalidOperationException("Option [{0}] is duplicated.".FormatWith(OptionRepository));
                    }

                    repository = option.Value;
                }
            }

            if (string.IsNullOrEmpty(result.AppName))
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionAppName));
            }

            if (result.Sources.Count == 0)
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionSource));
            }

            if (repository.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionRepository));
            }

            return result;
        }

        private static GenerateCommand CreateGenerateCommand(IList<CommandOption> options, out string repository)
        {
            var result = new GenerateCommand();
            repository = null;

            foreach (var option in options)
            {
                if (OptionTo.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(result.To))
                    {
                        throw new InvalidOperationException("Option [{0}] is duplicated.".FormatWith(OptionTo));
                    }

                    result.To = FileTools.RootPath(option.Value);
                }
                else if (OptionAppName.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    result.AppNames.Add(option.Value);
                }
                else if (OptionRepository.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!repository.IsNullOrEmpty())
                    {
                        throw new InvalidOperationException("Option [{0}] is duplicated.".FormatWith(OptionRepository));
                    }

                    repository = option.Value;
                }
            }

            if (result.AppNames.Count == 0)
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionAppName));
            }

            if (result.To.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionTo));
            }

            if (repository.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionRepository));
            }

            return result;
        }
    }
}
