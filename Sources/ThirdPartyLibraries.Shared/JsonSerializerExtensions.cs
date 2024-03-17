using Newtonsoft.Json;

namespace ThirdPartyLibraries.Shared;

public static class JsonSerializerExtensions
{
    public static T JsonDeserialize<T>(this Stream content)
    {
        using (var reader = new StreamReader(content))
        using (var jsonReader = new JsonTextReader(reader))
        {
            return new JsonSerializer().Deserialize<T>(jsonReader)!;
        }
    }

    public static T JsonDeserialize<T>(this byte[] content)
    {
        return new JsonSerializer().Deserialize<T>(new JsonTextReader(new StreamReader(new MemoryStream(content))))!;
    }
}