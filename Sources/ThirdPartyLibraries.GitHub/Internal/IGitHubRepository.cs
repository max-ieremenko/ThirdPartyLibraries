using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ThirdPartyLibraries.GitHub.Internal;

internal interface IGitHubRepository
{
    Task<JObject?> GetAsJsonAsync(string url, CancellationToken token);
}