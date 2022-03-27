using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite;

namespace ThirdPartyLibraries
{
    public static class Program
    {
        private const int ExitCodeOk = 0;
        private const int ExitCodeInvalidCommandLine = 1;
        private const int ExitCodeExecutionErrors = 2;
        private const int ExitCodeCommandError = 3;
        private const int ExitCodeTerminated = 4;

        public static Task<int> Main(string[] args)
        {
            var logger = new ConsoleLogger();

            CommandLine commandLine;
            try
            {
                commandLine = CommandLine.Parse(args);
            }
            catch (Exception ex)
            {
                return Task.FromResult(HandleError(ex, "Invalid command line: {0}", logger, ExitCodeInvalidCommandLine));
            }

            return RunAsync(commandLine, logger, CancellationToken.None);
        }

        private static async Task<int> RunAsync(CommandLine commandLine, ILogger logger, CancellationToken token)
        {
            ICommand command;
            string repository;
            try
            {
                command = CommandFactory.Create(commandLine, out repository);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Invalid command line: {0}", logger, ExitCodeInvalidCommandLine);
            }

            ServiceProvider serviceProvider;
            try
            {
                var services = new ServiceCollection();
                if (!string.IsNullOrEmpty(repository))
                {
                    await AppModule.AddConfigurationAsync(services, repository, token).ConfigureAwait(false);
                }

                ConfigureServices(services, logger);
                serviceProvider = services.BuildServiceProvider(true);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Application initialization error: {0}", logger, ExitCodeExecutionErrors);
            }

            bool commandResult;
            await using (serviceProvider.ConfigureAwait(false))
            {
                try
                {
                    commandResult = await command.ExecuteAsync(serviceProvider, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return HandleError(ex, null, logger, ExitCodeExecutionErrors);
                }
            }

            return commandResult ? ExitCodeOk : ExitCodeCommandError;
        }

        private static int HandleError(Exception ex, string messageFormat, ILogger logger, int defaultExistCode)
        {
            if (ex is OperationCanceledException)
            {
                logger.Error("Execution has been canceled by user.");
                return ExitCodeTerminated;
            }

            if (messageFormat == null)
            {
                logger.Error(ex.Message);
                logger.Info(ex.ToString());
            }
            else
            {
                logger.Error(messageFormat.FormatWith(ex.Message));
            }

            return defaultExistCode;
        }

        private static void ConfigureServices(IServiceCollection services, ILogger logger)
        {
            services.AddSingleton(logger);

            AppModule.ConfigureServices(services);
            Suite.AppModule.ConfigureServices(services);
            NuGet.AppModule.ConfigureServices(services);
            Npm.AppModule.ConfigureServices(services);
            GitHub.AppModule.ConfigureServices(services);
            Generic.AppModule.ConfigureServices(services);
        }
    }
}
