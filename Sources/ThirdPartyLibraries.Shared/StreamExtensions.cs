using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Shared;

public static class StreamExtensions
{
    public static async Task<byte[]> ToArrayAsync(this Stream stream, CancellationToken token)
    {
        stream.AssertNotNull(nameof(stream));

        if (stream is MemoryStream m)
        {
            return m.ToArray();
        }

        using (var result = new MemoryStream())
        {
            await stream.CopyToAsync(result, token).ConfigureAwait(false);
            return result.ToArray();
        }
    }
}