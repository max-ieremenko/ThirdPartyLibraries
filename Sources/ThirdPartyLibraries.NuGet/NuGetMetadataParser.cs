using System.IO;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet;

internal static class NuGetMetadataParser
{
    public static NuGetMetadata Parse(Stream stream)
    {
        var content = stream.JsonDeserialize<JObject>();

        return new NuGetMetadata(content.Value<int>("version"), content.Value<string>("source"));
    }
}