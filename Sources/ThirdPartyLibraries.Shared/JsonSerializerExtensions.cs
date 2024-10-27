using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ThirdPartyLibraries.Shared;

public static class JsonSerializerExtensions
{
    public static ValueTask<T?> JsonDeserializeAsync<T>(this Stream content, JsonTypeInfo<T> jsonTypeInfo, CancellationToken token) =>
        JsonSerializer.DeserializeAsync(content, jsonTypeInfo, token);

    public static T JsonDeserialize<T>(this Stream content, JsonTypeInfo<T> jsonTypeInfo) => JsonSerializer.Deserialize(content, jsonTypeInfo)!;
}