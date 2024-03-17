namespace ThirdPartyLibraries.PowerShell.Internal;

internal partial class Dispatcher
{
    private interface IWorkItem
    {
        void Invoke();
    }

    private sealed class WorkItem<T> : IWorkItem
    {
        private readonly Action<T> _action;
        private readonly T _arg;

        public WorkItem(Action<T> action, T arg)
        {
            _action = action;
            _arg = arg;
        }

        public void Invoke() => _action(_arg);
    }
}