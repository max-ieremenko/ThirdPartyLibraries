using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Suite.Internal;

[TestFixture]
public class HttpClientFactoryTest
{
    private SkipCertificateCheckConfiguration _configuration;

    [SetUp]
    public void BeforeEachTest()
    {
        _configuration = new SkipCertificateCheckConfiguration();
    }

    [Test]
    public void UserAgent()
    {
        var client = new HttpClientFactory(_configuration).CreateHttpClient();

        client.DefaultRequestHeaders.UserAgent.ShouldNotBeEmpty();
    }

    [Test]
    public void IsTransient()
    {
        var sut = new HttpClientFactory(_configuration);

        var instance1 = sut.CreateHttpClient();
        instance1.ShouldNotBeNull();

        var instance2 = sut.CreateHttpClient();
        instance2.ShouldNotBeNull();

        ReferenceEquals(instance1, instance2).ShouldBeFalse();
    }

    [Test]
    public void DefaultServerCertificateValidation()
    {
        var client = new HttpClientFactory(_configuration).CreateHttpClient();
        var handler = GetHandler(client);
        handler.ServerCertificateCustomValidationCallback.ShouldBeNull();
    }

    [Test]
    public void CustomServerCertificateValidation()
    {
        _configuration.ByHost = new[] { "*." };

        var client = new HttpClientFactory(_configuration).CreateHttpClient();
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

        var sut = new HttpClientFactory(_configuration);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        sut.ValidateServerCertificate(request, null, null, errors).ShouldBe(expected);
    }

    private static HttpClientHandler GetHandler(HttpClient client)
    {
        var field = typeof(HttpMessageInvoker)
            .GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        field.ShouldNotBeNull();

        return field.GetValue(client).ShouldBeOfType<HttpClientHandler>();
    }
}