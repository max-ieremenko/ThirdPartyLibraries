using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.GitHub.Internal;

[JsonSerializable(typeof(DummyResponse))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
internal sealed partial class DummyJsonSerializerContext : JsonSerializerContext;