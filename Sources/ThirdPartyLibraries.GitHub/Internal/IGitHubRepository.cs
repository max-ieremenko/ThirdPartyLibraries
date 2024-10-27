using System.Text.Json.Serialization.Metadata;

namespace ThirdPartyLibraries.GitHub.Internal;

internal interface IGitHubRepository
{
    Task<T?> GetAsJsonAsync<T>(string url, JsonTypeInfo<T> jsonTypeInfo, CancellationToken token);
}