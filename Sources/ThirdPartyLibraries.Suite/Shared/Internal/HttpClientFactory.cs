using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Configuration;

namespace ThirdPartyLibraries.Suite.Shared.Internal;

internal sealed class HttpClientFactory
{
    private static readonly ProductHeaderValue UserAgent =
        new("ThirdPartyLibraries", typeof(HttpClientFactory).Assembly.GetName().Version.ToString());

    private readonly SkipCertificateCheckConfiguration _configuration;
    private readonly ILogger _logger;
    private readonly Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>? _validateServerCertificateCallback;

    public HttpClientFactory(IOptions<SkipCertificateCheckConfiguration> configuration, ILogger logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
        _validateServerCertificateCallback = _configuration.ByHost.Length == 0 ? null : ValidateServerCertificate;
    }

    public HttpClient CreateHttpClient()
    {
        var client = new HttpClient(CreateHandler());

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

    private HttpClientHandler CreateHandler()
    {
        var result = _configuration.LogRequest ? new LoggerHandler(_logger) : new HttpClientHandler();
        result.ServerCertificateCustomValidationCallback = _validateServerCertificateCallback;
        return result;
    }

    private sealed class LoggerHandler : HttpClientHandler
    {
        private readonly ILogger _logger;

        public LoggerHandler(ILogger logger)
        {
            _logger = logger;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (_logger.Indent())
            {
                _logger.Info($"{request.Method} {request.RequestUri}");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}