using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.Repository.Template;

[JsonSerializable(typeof(LicenseIndexJson))]
[JsonSerializable(typeof(LibraryIndexJson))]
[JsonSerializable(typeof(CustomLibraryIndexJson))]
[JsonSerializable(typeof(Application))]
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.General,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    UseStringEnumConverter = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class DomainJsonSerializerContext : JsonSerializerContext;