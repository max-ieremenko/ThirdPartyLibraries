using System;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries;

internal sealed class EventLogger : LoggerBase
{
    private readonly Action<string> _onInfo;

    public EventLogger(Action<string> onInfo)
    {
        _onInfo = onInfo;
    }

    protected override void OnInfo(string message)
    {
        _onInfo(message);
    }
}