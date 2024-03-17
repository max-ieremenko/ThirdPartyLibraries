using System.Collections.Concurrent;

namespace ThirdPartyLibraries.PowerShell.Internal;

internal sealed partial class Dispatcher
{
    private readonly ConcurrentQueue<IWorkItem> _workItems = new();

    public void BeginInvoke<T>(Action<T> action, T arg)
    {
        _workItems.Enqueue(new WorkItem<T>(action, arg));
    }

    public void DoEvents()
    {
        while (_workItems.TryDequeue(out var workItem))
        {
            workItem.Invoke();
        }
    }
}