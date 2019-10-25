using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite;
using Unity;

namespace ThirdPartyLibraries
{
    internal sealed class HelpCommand : ICommand
    {
        [Dependency]
        public ILogger Logger { get; set; }

        public string Command { get; set; }

        public Task ExecuteAsync(CancellationToken token)
        {
            var fileName = Command.IsNullOrEmpty() ? "CommandLine.txt" : "CommandLine.{0}.txt".FormatWith(Command);
            Logger.Info(LoadContent(fileName));

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
