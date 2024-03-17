using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite;

namespace ThirdPartyLibraries;

public static class Program
{
    private const int ExitCodeOk = 0;
    private const int ExitCodeInvalidCommandLine = 1;
    private const int ExitCodeExecutionErrors = 2;
    private const int ExitCodeTerminated = 3;

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
            return Task.FromResult(HandleConsoleError(ex, "Invalid command line: {0}", logger, ExitCodeInvalidCommandLine));
        }

        using (var tokenSource = new CancellationTokenSource())
        {
            Console.CancelKeyPress += (s, e) => tokenSource.Cancel();
            return RunConsoleAsync(commandLine, logger, tokenSource.Token);
        }
    }

    public static async Task RunAsync(
        string commandName,
        IList<(string Name, string Value)> commandOptions,
        Action<string> infoLogger,
        Action<string> warnLogger,
        CancellationToken token)
    {
        var commandLine = CommandLine.Parse(commandName, commandOptions);

        var configuration = new Dictionary<string, string?>();
        var command = CommandFactory.Create(commandLine, configuration, out var repository);

        var serviceProvider = await ConfigureServices(new EventLogger(infoLogger, warnLogger), repository, configuration, token).ConfigureAwait(false);

        await using (serviceProvider.ConfigureAwait(false))
        {
            await command.ExecuteAsync(serviceProvider, token).ConfigureAwait(false);
        }
    }

    private static async Task<int> RunConsoleAsync(CommandLine commandLine, ConsoleLogger logger, CancellationToken token)
    {
        var configuration = new Dictionary<string, string?>();
        ICommand command;
        string? repository;
        try
        {
            command = CommandFactory.Create(commandLine, configuration, out repository);
        }
        catch (Exception ex)
        {
            return HandleConsoleError(ex, "Invalid command line: {0}", logger, ExitCodeInvalidCommandLine);
        }

        ServiceProvider serviceProvider;
        try
        {
            serviceProvider = await ConfigureServices(logger, repository, configuration, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return HandleConsoleError(ex, "Application initialization error: {0}", logger, ExitCodeExecutionErrors);
        }

        await using (serviceProvider.ConfigureAwait(false))
        {
            try
            {
                await command.ExecuteAsync(serviceProvider, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return HandleConsoleError(ex, null, logger, ExitCodeExecutionErrors);
            }
        }

        return ExitCodeOk;
    }

    private static int HandleConsoleError(Exception ex, string? messageFormat, ConsoleLogger logger, int defaultExistCode)
    {
        if (ex is OperationCanceledException)
        {
            logger.Error("The execution was canceled by the user.");
            return ExitCodeTerminated;
        }

        if (ex is IApplicationException app)
        {
            logger.Error(app);
            return defaultExistCode;
        }

        if (messageFormat == null)
        {
            logger.Error(ex.Message);
            logger.Info(ex.ToString());
        }
        else
        {
            logger.Error(string.Format(messageFormat, ex.Message));
        }

        return defaultExistCode;
    }

    private static async Task<ServiceProvider> ConfigureServices(
        ILogger logger,
        string? repository,
        Dictionary<string, string?> commandLine,
        CancellationToken token)
    {
        var services = new ServiceCollection();
        services.AddSingleton(logger);

        var configuration = await ConfigurationFactory.CreateAsync(services, repository, commandLine, token).ConfigureAwait(false);

        Generic.AppModule.ConfigureServices(services, configuration);
        GitHub.AppModule.ConfigureServices(services, configuration);
        Suite.AppModule.ConfigureServices(services, configuration);
        NuGet.AppModule.ConfigureServices(services, configuration);
        Npm.AppModule.ConfigureServices(services, configuration);

        return services.BuildServiceProvider(true);
    }
}