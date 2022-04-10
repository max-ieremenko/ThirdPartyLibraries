using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.GitHub
{
    internal sealed class GitHubApi : IGitHubApi
    {
        public const string Host = "https://" + KnownHosts.GitHubApi;

        public GitHubApi(Func<HttpClient> httpClientFactory)
        {
            httpClientFactory.AssertNotNull(nameof(httpClientFactory));
         
            HttpClientFactory = httpClientFactory;
        }

        public Func<HttpClient> HttpClientFactory { get; }

        public async Task<GitHubLicense?> LoadLicenseAsync(string licenseUrl, string authorizationToken, CancellationToken token)
        {
            licenseUrl.AssertNotNull(nameof(licenseUrl));

            var url = GetLicenseUrl(new Uri(licenseUrl));
            if (url.IsNullOrEmpty())
            {
                return null;
            }

            var content = await RequestLicenseContentAsync(url, authorizationToken, token).ConfigureAwait(false);
            if (content == null)
            {
                return null;
            }

            var encoding = content.Value<string>("encoding");
            if (!"base64".EqualsIgnoreCase(encoding))
            {
                throw new NotSupportedException("GitHub encoding {0} is not supported.".FormatWith(encoding));
            }

            var result = new GitHubLicense
            {
                SpdxId = content.Value<JObject>("license").Value<string>("spdx_id"),
                SpdxIdHRef = url,
                FileName = content.Value<string>("name"),
                FileContentHRef = content.Value<string>("download_url"),
                FileContent = Convert.FromBase64String(content.Value<string>("content"))
            };

            if ("NOASSERTION".EqualsIgnoreCase(result.SpdxId))
            {
                result.SpdxId = null;
            }

            return result;
        }

        internal static string GetLicenseUrl(Uri url)
        {
            // https://
            var path = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (path.Length < 2)
            {
                return null;
            }

            var owner = path[0];
            var repository = path[1];
            if (url.Host.EqualsIgnoreCase(KnownHosts.GitHubApi))
            {
                owner = path[1];
                repository = path[2];
            }

            if (repository.EndsWith(".git", StringComparison.OrdinalIgnoreCase) && repository.Length > 4)
            {
                repository = repository.Substring(0, repository.Length - 4);
            }

            return Host + "/repos/{0}/{1}/license".FormatWith(owner, repository);
        }

        private static async Task<ApiRateLimitExceededException> TryGetLimitInfoAsync(HttpResponseMessage response)
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

        private async Task<JObject> RequestLicenseContentAsync(string url, string authorizationToken, CancellationToken token)
        {
            var client = HttpClientFactory();
            if (!authorizationToken.IsNullOrEmpty())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", authorizationToken);
            }

            JObject result;

            using (client)
            using (var response = await client.GetAsync(url, token).ConfigureAwait(false))
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
                using (var reader = new JsonTextReader(new StreamReader(stream)))
                {
                    result = (JObject)new JsonSerializer().Deserialize(reader);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException("GitHub personal access token is invalid: {0}".FormatWith(result.Value<string>("message")));
                }
            }

            return result;
        }
    }
}
