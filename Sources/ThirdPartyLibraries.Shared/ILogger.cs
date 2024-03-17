namespace ThirdPartyLibraries.Shared;

public interface ILogger
{
    void Info(string message);

    void Warn(string message);

    IDisposable Indent();
}