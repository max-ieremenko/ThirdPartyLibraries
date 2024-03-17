using Newtonsoft.Json;

namespace ThirdPartyLibraries;

public static class JsonSerializerExtensions
{
    public static string ToJsonString(this object content)
    {
        var json = new StringBuilder();
        using (var jsonWriter = new JsonTextWriter(new StringWriter(json)))
        {
            new JsonSerializer().Serialize(jsonWriter, content);
        }

        return json.ToString();
    }

    public static Stream JsonSerialize(this object content)
    {
        var result = new MemoryStream();

        using (var writer = new StreamWriter(result, null, -1, true))
        using (var jsonWriter = new JsonTextWriter(writer))
        {
            new JsonSerializer().Serialize(jsonWriter, content);
        }

        result.Position = 0;
        return result;
    }
}