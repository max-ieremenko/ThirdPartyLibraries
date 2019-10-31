using System;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries
{
    internal sealed class ConsoleLogger : ILogger
    {
        private string _indentation;

        public void Error(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            if (_indentation.IsNullOrEmpty())
            {
                Console.WriteLine("Error: " + message);
            }
            else
            {
                Console.WriteLine(_indentation + message);
            }

            Console.ForegroundColor = color;
        }

        public void Info(string message)
        {
            Console.WriteLine(_indentation + message);
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
    }
}
