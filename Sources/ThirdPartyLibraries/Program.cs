using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite;
using Unity;

namespace ThirdPartyLibraries
{
    public static class Program
    {
        private const int ExitCodeOk = 0;
        private const int ExitCodeInvalidCommandLine = 1;
        private const int ExitCodeExecutionErrors = 2;

        public static async Task<int> Main(string[] args)
        {
            var logger = new ConsoleLogger();
            var container = new UnityContainer();

            try
            {
                ConfigureContainer(container, logger);
            }
            catch (Exception ex)
            {
                logger.Error("Application initialization error: {0}".FormatWith(ex.Message));
                return ExitCodeExecutionErrors;
            }

            using (container)
            {
                ICommand command;
                try
                {
                    command = await ResolveCommandAsync(container, args, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.Error("Invalid command line: {0}".FormatWith(ex.Message));
                    return ExitCodeInvalidCommandLine;
                }

                try
                {
                    await command.ExecuteAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    logger.Info(ex.ToString());
                    return ExitCodeExecutionErrors;
                }
            }

            return ExitCodeOk;
        }

        private static Task<ICommand> ResolveCommandAsync(IUnityContainer container, string[] args, CancellationToken token)
        {
            var commandLine = CommandLine.Parse(args);
            return container.Resolve<CommandFactory>().CreateAsync(commandLine, token);
        }

        private static void ConfigureContainer(IUnityContainer container, ILogger logger)
        {
            container.RegisterInstance(logger);

            Suite.AppModule.ConfigureContainer(container);
            NuGet.AppModule.ConfigureContainer(container);
            GitHub.AppModule.ConfigureContainer(container);
            Generic.AppModule.ConfigureContainer(container);
        }
    }
}
