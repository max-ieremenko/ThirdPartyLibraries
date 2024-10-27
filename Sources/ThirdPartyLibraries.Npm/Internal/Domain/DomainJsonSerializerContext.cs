using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.Npm.Internal.Domain;

[JsonSerializable(typeof(NpmPackageIndex))]
[JsonSerializable(typeof(NpmPackageJson))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, AllowTrailingCommas = true)]
internal sealed partial class DomainJsonSerializerContext : JsonSerializerContext;