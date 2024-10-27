using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.GitHub.Internal.Domain;

[JsonSerializable(typeof(GitHubRepositoryLicense))]
[JsonSerializable(typeof(GitHubLicense))]
[JsonSerializable(typeof(GitHubError))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
internal sealed partial class DomainJsonSerializerContext : JsonSerializerContext;