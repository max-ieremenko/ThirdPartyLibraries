using System;

namespace ThirdPartyLibraries.Shared
{
    public interface ILogger
    {
        void Info(string message);

        IDisposable Indent();
    }
}
