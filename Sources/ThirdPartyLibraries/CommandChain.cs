using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Suite;

namespace ThirdPartyLibraries
{
    internal sealed class CommandChain : ICommand
    {
        public CommandChain(params ICommand[] chain)
        {
            Chain = chain;
        }

        public ICommand[] Chain { get; }

        public async Task ExecuteAsync(CancellationToken token)
        {
            foreach (var command in Chain)
            {
                await command.ExecuteAsync(token);
            }
        }
    }
}
