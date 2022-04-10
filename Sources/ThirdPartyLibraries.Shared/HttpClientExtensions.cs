using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace ThirdPartyLibraries.Shared
{
    public static class HttpClientExtensions
    {
        private static readonly ProductHeaderValue UserAgent =
            new ProductHeaderValue("ThirdPartyLibraries", typeof(HttpClientExtensions).Assembly.GetName().Version.ToString());

        public static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();

            // http://developer.github.com/v3/#user-agent-required
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
            return client;
        }

        public static async Task AssertStatusCodeOk(this HttpResponseMessage response)
        {
            response.AssertNotNull(nameof(response));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var error = new StringBuilder()
                    .AppendFormat("{0}: {1}", response.StatusCode, response.ReasonPhrase)
                    .AppendLine()
                    .AppendLine("----------------")
                    .Append(responseContent);

                throw new InvalidOperationException(error.ToString());
            }
        }

        public static async Task<TResult> GetAsJsonAsync<TResult>([NotNull] this HttpClient client, [NotNull] string requestUri, CancellationToken token)
        {
            client.AssertNotNull(nameof(client));
            requestUri.AssertNotNull(nameof(requestUri));

            using (var response = await client.GetAsync(requestUri, token).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return default;
                }

                await response.AssertStatusCodeOk().ConfigureAwait(false);

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new JsonTextReader(new StreamReader(stream)))
                {
                    return new JsonSerializer().Deserialize<TResult>(reader);
                }
            }
        }
    }
}
