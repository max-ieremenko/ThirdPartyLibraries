namespace ThirdPartyLibraries.PowerShell.Internal;

internal interface ICmdLetLogger
{
    void Info(string message);

    void Warn(string message);

    void Error(Exception exception);
}