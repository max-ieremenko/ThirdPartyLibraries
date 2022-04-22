using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal.GenericAdapters;

namespace ThirdPartyLibraries.Suite.Internal;

internal sealed class HttpClientFactory
{
    private static readonly ProductHeaderValue UserAgent =
        new("ThirdPartyLibraries", typeof(HttpClientFactory).Assembly.GetName().Version.ToString());

    private readonly SkipCertificateCheckConfiguration _configuration;
    private readonly Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> _validateServerCertificateCallback;

    public HttpClientFactory(SkipCertificateCheckConfiguration configuration)
    {
        _configuration = configuration;
        _validateServerCertificateCallback = _configuration.ByHost.IsNullOrEmpty() ? null : ValidateServerCertificate;
    }

    public HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = _validateServerCertificateCallback
        };

        var client = new HttpClient(handler);

        // http://developer.github.com/v3/#user-agent-required
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
        return client;
    }

    internal bool ValidateServerCertificate(HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors)
    {
        if (errors == SslPolicyErrors.None)
        {
            return true;
        }

        var host = request.RequestUri?.Host;
        if (string.IsNullOrEmpty(host))
        {
            return false;
        }

        return new IgnoreFilter(_configuration.ByHost).Filter(host);
    }
}