using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Suite
{
    public interface ICommand
    {
        ValueTask<bool> ExecuteAsync(CancellationToken token);
    }
}
