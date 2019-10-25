using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite;
using ThirdPartyLibraries.Suite.Commands;
using Unity;

namespace ThirdPartyLibraries
{
    internal sealed class CommandFactory
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

        private const string UserSecretsId = "c903410c-3d05-49fe-bc8b-b95a2f4dfc69";
        private const string EnvironmentVariablePrefix = "ThirdPartyLibraries:";

        [Dependency]
        public IUnityContainer Container { get; set; }

        public async Task<ICommand> CreateAsync(CommandLine line, CancellationToken token)
        {
            if (string.IsNullOrEmpty(line.Command))
            {
                return CreateHelp(null);
            }

            ICommand result;
            string repository;
            if (CommandUpdate.EqualsIgnoreCase(line.Command))
            {
                if (IsHelp(line.Options))
                {
                    return CreateHelp(CommandUpdate);
                }

                var command = CreateUpdateCommand(line.Options, out repository);
                result = new CommandChain(command, CreateRefreshCommand());
            }
            else if (CommandRefresh.EqualsIgnoreCase(line.Command))
            {
                if (IsHelp(line.Options))
                {
                    return CreateHelp(CommandRefresh);
                }

                result = CreateRefreshCommand(line.Options, out repository);
            }
            else if (CommandValidate.EqualsIgnoreCase(line.Command))
            {
                if (IsHelp(line.Options))
                {
                    return CreateHelp(CommandValidate);
                }

                result = CreateValidateCommand(line.Options, out repository);
            }
            else if (CommandGenerate.EqualsIgnoreCase(line.Command))
            {
                if (IsHelp(line.Options))
                {
                    return CreateHelp(CommandValidate);
                }

                result = CreateGenerateCommand(line.Options, out repository);
            }
            else
            {
                throw new InvalidOperationException("Unknown command [{0}].".FormatWith(line.Command));
            }

            await InitializeConfigurationAsync(repository, token);
            return result;
        }

        private static bool IsHelp(IList<CommandOption> options)
        {
            return options.Count == 0 || (options.Count == 1 && OptionHelp.Equals(options[0].Name));
        }

        private ICommand CreateHelp(string command)
        {
            var result = Container.Resolve<HelpCommand>();
            result.Command = command;
            return result;
        }

        private UpdateCommand CreateUpdateCommand(IList<CommandOption> options, out string repository)
        {
            var result = Container.Resolve<UpdateCommand>();
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

                    repository = FileTools.RootPath(option.Value);
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

        private RefreshCommand CreateRefreshCommand(IList<CommandOption> options, out string repository)
        {
            var result = CreateRefreshCommand();
            repository = null;

            foreach (var option in options)
            {
                if (OptionRepository.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!repository.IsNullOrEmpty())
                    {
                        throw new InvalidOperationException("Option [{0}] is duplicated.".FormatWith(OptionRepository));
                    }

                    repository = FileTools.RootPath(option.Value);
                }
            }

            if (repository.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Missing option [{0}].".FormatWith(OptionRepository));
            }

            return result;
        }

        private RefreshCommand CreateRefreshCommand()
        {
            return Container.Resolve<RefreshCommand>();
        }

        private ValidateCommand CreateValidateCommand(IList<CommandOption> options, out string repository)
        {
            var result = Container.Resolve<ValidateCommand>();
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

                    repository = FileTools.RootPath(option.Value);
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

        private GenerateCommand CreateGenerateCommand(IList<CommandOption> options, out string repository)
        {
            var result = Container.Resolve<GenerateCommand>();
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

                    repository = FileTools.RootPath(option.Value);
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

        private async Task InitializeConfigurationAsync(string repository, CancellationToken token)
        {
            var storage = StorageFactory.Create(repository);
            Container.RegisterInstance(storage);

            IConfigurationRoot configuration;
            using (var settings = await storage.GetOrCreateAppSettingsAsync(token))
            {
                configuration = new ConfigurationBuilder()
                    .AddJsonStream(settings)
                    .AddUserSecrets(UserSecretsId)
                    .AddEnvironmentVariables(prefix: EnvironmentVariablePrefix)
                    .Build();
            }

            Container.RegisterInstance<IConfigurationManager>(new ConfigurationManager(configuration));
        }
    }
}
