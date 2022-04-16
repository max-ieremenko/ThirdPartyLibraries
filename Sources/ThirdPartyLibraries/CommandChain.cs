using System;
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

        public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            for (var i = 0; i < Chain.Length; i++)
            {
                var command = Chain[i];
                token.ThrowIfCancellationRequested();

                await command.ExecuteAsync(serviceProvider, token).ConfigureAwait(false);
            }
        }
    }
}
