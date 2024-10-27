using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Options;
using ThirdPartyLibraries.GitHub.Configuration;
using ThirdPartyLibraries.GitHub.Internal.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.GitHub.Internal;

internal class GitHubRepository : IGitHubRepository
{
    private readonly Func<HttpClient> _httpClientFactory;
    private readonly GitHubConfiguration _configuration;

    public GitHubRepository(IOptions<GitHubConfiguration> configuration, Func<HttpClient> httpClientFactory)
    {
        _configuration = configuration.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<T?> GetAsJsonAsync<T>(string url, JsonTypeInfo<T> jsonTypeInfo, CancellationToken token)
    {
        var client = _httpClientFactory();
        if (!string.IsNullOrWhiteSpace(_configuration.PersonalAccessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _configuration.PersonalAccessToken);
        }

        T? result;

        using (client)
        using (var response = await client.InvokeGetAsync(url, token).ConfigureAwait(false))
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                var ex = await TryGetLimitInfoAsync(response).ConfigureAwait(false);
                if (ex != null)
                {
                    throw ex;
                }
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var error = await stream.JsonDeserializeAsync(DomainJsonSerializerContext.Default.GitHubError, token).ConfigureAwait(false);
                    throw new InvalidOperationException($"GitHub personal access token is invalid: {error?.Message}");
                }
            }

            await response.AssertStatusCodeOk().ConfigureAwait(false);
            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                result = await stream.JsonDeserializeAsync(jsonTypeInfo, token).ConfigureAwait(false);
            }
        }

        return result;
    }

    private static async Task<ApiRateLimitExceededException?> TryGetLimitInfoAsync(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetInt64Value("X-RateLimit-Limit", out var limit)
            || !response.Headers.TryGetInt64Value("X-RateLimit-Remaining", out var remaining)
            || !response.Headers.TryGetInt64Value("X-RateLimit-Reset", out var reset))
        {
            return null;
        }

        var window = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMilliseconds(reset * 1000);
        var message = new StringBuilder()
            .Append(response.StatusCode)
            .Append(": ")
            .Append(response.ReasonPhrase)
            .Append(". ")
            .Append(remaining)
            .Append(" of ")
            .Append(limit)
            .Append(" requests remaining in the current rate limit window ")
            .Append(window.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture))
            .AppendLine()
            .AppendLine("----------------")
            .Append(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

        return new ApiRateLimitExceededException(message.ToString(), limit, remaining, window);
    }
}