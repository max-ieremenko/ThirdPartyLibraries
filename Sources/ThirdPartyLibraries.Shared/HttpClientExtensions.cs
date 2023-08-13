using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyLibraries.Shared;

public static class HttpClientExtensions
{
    public static async Task AssertStatusCodeOk(this HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.OK)
        {
            return;
        }

        string? responseContent = null;

        try
        {
            responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch
        {
        }

        var error = new StringBuilder()
            .AppendFormat("Unable to access {0}: {1} - {2}", response.RequestMessage?.RequestUri, response.StatusCode, response.ReasonPhrase);

        if (!string.IsNullOrWhiteSpace(responseContent))
        {
            error
                .AppendLine()
                .AppendLine("----------------")
                .Append(responseContent);
        }

        throw new HttpRequestException(error.ToString());
    }

    public static async Task<HttpResponseMessage> InvokeGetAsync(this HttpClient client, string requestUri, CancellationToken token)
    {
        try
        {
            return await client.GetAsync(requestUri, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Unable to access {requestUri}: {ex.Message}", ex);
        }
    }

    public static async Task<TResult?> GetAsJsonAsync<TResult>(this HttpClient client, string requestUri, CancellationToken token)
    {
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

    public static async Task<(string Extension, byte[] Content)?> GetFileAsync(this HttpClient client, string requestUri, CancellationToken token)
    {
        string mediaType;
        byte[] content;

        using (var response = await client.InvokeGetAsync(requestUri, token).ConfigureAwait(false))
        {
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            mediaType = response.Content.Headers.ContentType.MediaType;
            content = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        return (ResolveExtension(mediaType), content);
    }

    private static string ResolveExtension(string mediaType)
    {
        if (MediaTypeNames.Text.Html.Equals(mediaType, StringComparison.OrdinalIgnoreCase))
        {
            return ".html";
        }

        if (MediaTypeNames.Text.RichText.Equals(mediaType, StringComparison.OrdinalIgnoreCase))
        {
            return ".rtf";
        }

        return ".txt";
    }
}