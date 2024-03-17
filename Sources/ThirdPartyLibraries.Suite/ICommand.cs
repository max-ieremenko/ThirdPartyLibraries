namespace ThirdPartyLibraries.Suite;

public interface ICommand
{
    Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token);
}