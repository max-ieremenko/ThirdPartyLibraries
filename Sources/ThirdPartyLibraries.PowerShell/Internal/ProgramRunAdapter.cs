using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.PowerShell.Internal;

internal sealed class ProgramRunAdapter
{
    private readonly ICmdLetLogger _logger;
    private readonly Dispatcher _dispatcher;

    public ProgramRunAdapter(ICmdLetLogger logger)
    {
        _logger = logger;
        _dispatcher = new Dispatcher();
    }

    public void OnInfo(string message)
    {
        _dispatcher.BeginInvoke(_logger.Info, message);
    }

    public void OnWarn(string message)
    {
        _dispatcher.BeginInvoke(_logger.Warn, message);
    }

    public void Wait(Task task)
    {
        while (!task.IsCompleted)
        {
            _dispatcher.DoEvents();
            Thread.Sleep(500);
        }

        _dispatcher.DoEvents();
        
        if (task.IsFaulted)
        {
            var flatten = task.Exception.Flatten();
            _logger.Error(flatten.InnerExceptions.Count == 1 ? flatten.InnerException : flatten);
        }
        else if (task.IsCanceled)
        {
            _logger.Warn("The execution was canceled by the user.");
        }
    }
}