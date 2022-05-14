using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ThirdPartyLibraries.Shared;

public static class HttpClientExtensions
{
    public static async Task AssertStatusCodeOk(this HttpResponseMessage response)
    {
        response.AssertNotNull(nameof(response));

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return;
        }

        string responseContent = null;

        if (response.Content != null)
        {
            try
            {
                responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        }

        var error = new StringBuilder()
            .AppendFormat("Unable to access {0}: {1} - {2}", response.RequestMessage.RequestUri, response.StatusCode, response.ReasonPhrase);

        if (!string.IsNullOrWhiteSpace(responseContent))
        {
            error
                .AppendLine()
                .AppendLine("----------------")
                .Append(responseContent);
        }

        throw new HttpRequestException(error.ToString());
    }

    public static async Task<HttpResponseMessage> InvokeGetAsync([NotNull] this HttpClient client, [NotNull] string requestUri, CancellationToken token)
    {
        client.AssertNotNull(nameof(client));
        requestUri.AssertNotNull(nameof(requestUri));

        try
        {
            return await client.GetAsync(requestUri, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException("Unable to access {0}: {1}".FormatWith(requestUri, ex.Message), ex);
        }
    }

    public static async Task<TResult> GetAsJsonAsync<TResult>([NotNull] this HttpClient client, [NotNull] string requestUri, CancellationToken token)
    {
        client.AssertNotNull(nameof(client));
        requestUri.AssertNotNull(nameof(requestUri));

        using (var response = await client.InvokeGetAsync(requestUri, token).ConfigureAwait(false))
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            await response.AssertStatusCodeOk().ConfigureAwait(false);

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                return stream.JsonDeserialize<TResult>();
            }
        }
    }
}