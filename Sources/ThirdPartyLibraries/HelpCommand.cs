using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite;

namespace ThirdPartyLibraries
{
    internal sealed class HelpCommand : ICommand
    {
        public HelpCommand(string command)
        {
            Command = command;
        }

        public string Command { get; internal set; }

        public Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            var suffix = Command.IsNullOrEmpty() ? "default" : Command;
            var fileName = "CommandLine.{0}.txt".FormatWith(suffix);
            serviceProvider.GetRequiredService<ILogger>().Info(LoadContent(fileName));

            return Task.CompletedTask;
        }

        private static string LoadContent(string fileName)
        {
            var scope = typeof(CommandLine);

            using (var stream = scope.Assembly.GetManifestResourceStream(scope, fileName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
