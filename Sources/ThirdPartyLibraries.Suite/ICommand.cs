using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Suite
{
    public interface ICommand
    {
        Task ExecuteAsync(CancellationToken token);
    }
}
