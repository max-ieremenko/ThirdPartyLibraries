using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries;

internal sealed class EventLogger : LoggerBase
{
    private readonly Action<string> _onInfo;
    private readonly Action<string> _onWarn;

    public EventLogger(Action<string> onInfo, Action<string> onWarn)
    {
        _onInfo = onInfo;
        _onWarn = onWarn;
    }

    protected override void OnInfo(string message)
    {
        _onInfo(message);
    }

    protected override void OnWarn(string message)
    {
        _onWarn(message);
    }
}