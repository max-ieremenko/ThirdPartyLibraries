using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet.Internal;

internal static class NuGetMetadataParser
{
    public static bool TryGetSource(string fileName, [NotNullWhen(true)] out Uri? source)
    {
        using (var stream = File.OpenRead(fileName))
        {
            return TryGetSource(stream, out source);
        }
    }

    public static bool TryGetSource(Stream stream, [NotNullWhen(true)] out Uri? source)
    {
        var content = stream.JsonDeserialize<JObject>();
        var path = content.Value<string>("source");
        return Uri.TryCreate(path, UriKind.Absolute, out source);
    }
}