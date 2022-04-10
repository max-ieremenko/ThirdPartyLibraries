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

        public async ValueTask<bool> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            var result = true;
            foreach (var command in Chain)
            {
                token.ThrowIfCancellationRequested();

                var flag = await command.ExecuteAsync(serviceProvider, token).ConfigureAwait(false);
                if (!flag)
                {
                    result = false;
                }
            }

            return result;
        }
    }
}
