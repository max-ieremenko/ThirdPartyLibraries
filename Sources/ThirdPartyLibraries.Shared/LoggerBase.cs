using System;

namespace ThirdPartyLibraries.Shared;

public abstract class LoggerBase : ILogger
{
    private string _indentation;

    public void Info(string message)
    {
        OnInfo(_indentation + message);
    }

    public void Warn(string message)
    {
        OnWarn(_indentation + message);
    }

    public IDisposable Indent()
    {
        const int IndentValue = 3;
        const char IndentChar = ' ';

        _indentation += new string(IndentChar, IndentValue);
        return new DisposableAction(() =>
        {
            var length = (_indentation.Length / IndentValue) - 1;
            _indentation = length == 0 ? null : new string(IndentChar, length * IndentValue);
        });
    }

    protected string GetIndentation() => _indentation;

    protected abstract void OnInfo(string message);

    protected abstract void OnWarn(string message);
}