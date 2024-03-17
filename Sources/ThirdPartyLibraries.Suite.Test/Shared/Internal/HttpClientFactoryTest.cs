using System.Net.Security;
using System.Reflection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Configuration;

namespace ThirdPartyLibraries.Suite.Shared.Internal;

[TestFixture]
public class HttpClientFactoryTest
{
    private SkipCertificateCheckConfiguration _configuration = null!;
    private ILogger _logger = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        var logger = new Mock<ILogger>(MockBehavior.Strict);
        logger
            .Setup(l => l.Indent())
            .Returns((IDisposable)null!);
        logger
            .Setup(l => l.Info(It.IsAny<string>()));

        _logger = logger.Object;
        _configuration = new SkipCertificateCheckConfiguration();
    }

    [Test]
    public void UserAgent()
    {
        var client = CreateSut().CreateHttpClient();

        client.DefaultRequestHeaders.UserAgent.ShouldNotBeEmpty();
    }

    [Test]
    public void DefaultServerCertificateValidation()
    {
        var client = CreateSut().CreateHttpClient();
        var handler = GetHandler(client);
        handler.ServerCertificateCustomValidationCallback.ShouldBeNull();
    }

    [Test]
    public void CustomServerCertificateValidation()
    {
        _configuration.ByHost = new[] { "*." };

        var client = CreateSut().CreateHttpClient();
        var handler = GetHandler(client);
        handler.ServerCertificateCustomValidationCallback.ShouldNotBeNull();
    }

    [Test]
    [TestCase("http://ignore", "does not matter", SslPolicyErrors.None, true)]
    [TestCase("http://host.com", "host\\.com", SslPolicyErrors.RemoteCertificateChainErrors, true)]
    [TestCase("http://host.com", "host\\.com1", SslPolicyErrors.RemoteCertificateNotAvailable, false)]
    public void ValidateServerCertificate(string requestUrl, string filter, SslPolicyErrors errors, bool expected)
    {
        _configuration.ByHost = new[] { filter };

        var sut = CreateSut();
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        sut.ValidateServerCertificate(request, null!, null!, errors).ShouldBe(expected);
    }

    private static HttpClientHandler GetHandler(HttpClient client)
    {
        var field = typeof(HttpMessageInvoker)
            .GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        field.ShouldNotBeNull();

        return field.GetValue(client).ShouldBeAssignableTo<HttpClientHandler>()!;
    }

    private HttpClientFactory CreateSut() => new(new OptionsWrapper<SkipCertificateCheckConfiguration>(_configuration), _logger);
}