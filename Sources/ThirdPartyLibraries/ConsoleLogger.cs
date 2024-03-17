using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries;

internal sealed class ConsoleLogger : LoggerBase
{
    public void Error(IApplicationException exception)
    {
        using (new WithForegroundColor(ConsoleColor.Red))
        {
            exception.Log(this);
        }
    }

    public void Error(string message)
    {
        using (new WithForegroundColor(ConsoleColor.Red))
        {
            Console.WriteLine(GetIndentation() + message);
        }
    }

    protected override void OnInfo(string message)
    {
        Console.WriteLine(message);
    }

    protected override void OnWarn(string message)
    {
        using (new WithForegroundColor(ConsoleColor.Yellow))
        {
            Console.WriteLine(message);
        }
    }

    private readonly ref struct WithForegroundColor
    {
        private readonly ConsoleColor _original;

        public WithForegroundColor(ConsoleColor color)
        {
            _original = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }

        public void Dispose()
        {
            Console.ForegroundColor = _original;
        }
    }
}