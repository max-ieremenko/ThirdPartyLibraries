using System;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries
{
    internal sealed class ConsoleLogger : LoggerBase
    {
        public void Error(IApplicationException exception)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            exception.Log(this);

            Console.ForegroundColor = color;
        }

        public void Error(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(GetIndentation() + message);

            Console.ForegroundColor = color;
        }

        protected override void OnInfo(string message)
        {
            Console.WriteLine(message);
        }
    }
}
