using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.GitHub.Configuration;
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

    public async Task<JObject?> GetAsJsonAsync(string url, CancellationToken token)
    {
        var client = _httpClientFactory();
        if (!string.IsNullOrWhiteSpace(_configuration.PersonalAccessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _configuration.PersonalAccessToken);
        }

        JObject result;

        using (client)
        using (var response = await client.InvokeGetAsync(url, token).ConfigureAwait(false))
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                var ex = await TryGetLimitInfoAsync(response).ConfigureAwait(false);
                if (ex != null)
                {
                    throw ex;
                }
            }

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                await response.AssertStatusCodeOk().ConfigureAwait(false);
            }

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                result = stream.JsonDeserialize<JObject>();
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var message = result.Value<string>("message");
                throw new InvalidOperationException($"GitHub personal access token is invalid: {message}");
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