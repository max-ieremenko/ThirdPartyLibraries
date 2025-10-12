using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.Generic.Internal.Domain;

[JsonSerializable(typeof(OsiApprovedLicense[]))]
[JsonSerializable(typeof(SpdxOrgLicense))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
internal sealed partial class DomainJsonSerializerContext : JsonSerializerContext;