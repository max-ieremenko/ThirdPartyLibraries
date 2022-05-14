using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

namespace ThirdPartyLibraries.Shared;

[TestFixture]
public class HttpClientExtensionsTest
{
    private MockHttpMessageHandler _mockHttp;

    [SetUp]
    public void BeforeEachTest()
    {
        _mockHttp = new MockHttpMessageHandler();
    }

    [Test]
    public async Task AssertStatusCodeOkSuccess()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://host.com")
            .Respond(HttpStatusCode.OK);

        using (var client = _mockHttp.ToHttpClient())
        using (var response = await client.GetAsync("https://host.com").ConfigureAwait(false))
        {
            await response.AssertStatusCodeOk().ConfigureAwait(false);
        }
    }

    [Test]
    public async Task AssertStatusCodeOkFail()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://host.com")
            .Respond(
                HttpStatusCode.NotFound,
                MediaTypeNames.Application.Json,
                "some content");

        using (var client = _mockHttp.ToHttpClient())
        using (var response = await client.GetAsync("https://host.com").ConfigureAwait(false))
        {
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => response.AssertStatusCodeOk());

            ex.Message.ShouldContain("https://host.com");
            ex.Message.ShouldContain("NotFound");
            ex.Message.ShouldContain("some content");
            ex.InnerException.ShouldBeNull();
        }
    }

    [Test]
    public async Task InvokeGetAsyncSuccess()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://host.com")
            .Respond(HttpStatusCode.OK);

        using (var client = _mockHttp.ToHttpClient())
        using (var response = await client.InvokeGetAsync("https://host.com", CancellationToken.None).ConfigureAwait(false))
        {
            response.ShouldNotBeNull();
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Test]
    public void InvokeGetAsyncFail()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://host.com")
            .Respond(() => throw new NotSupportedException("oops!"));

        using (var client = _mockHttp.ToHttpClient())
        {
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => client.InvokeGetAsync("https://host.com", CancellationToken.None));

            ex.Message.ShouldContain("https://host.com");
            ex.Message.ShouldContain("oops!");
            ex.InnerException.ShouldBeOfType<NotSupportedException>();
        }
    }
}