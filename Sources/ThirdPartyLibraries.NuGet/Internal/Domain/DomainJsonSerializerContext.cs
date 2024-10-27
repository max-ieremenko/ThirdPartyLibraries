using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.NuGet.Internal.Domain;

[JsonSerializable(typeof(ProjectAssetsJson))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, AllowTrailingCommas = true, NumberHandling = JsonNumberHandling.AllowReadingFromString)]
internal sealed partial class DomainJsonSerializerContext : JsonSerializerContext;