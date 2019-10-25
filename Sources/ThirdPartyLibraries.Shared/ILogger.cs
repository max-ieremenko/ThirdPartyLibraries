using System;

namespace ThirdPartyLibraries.Shared
{
    public interface ILogger
    {
        void Error(string message);

        void Info(string message);

        IDisposable Indent();
    }
}
