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

        public async ValueTask<bool> ExecuteAsync(CancellationToken token)
        {
            var result = true;
            foreach (var command in Chain)
            {
                var flag = await command.ExecuteAsync(token);
                if (!flag)
                {
                    result = false;
                }
            }

            return result;
        }
    }
}
