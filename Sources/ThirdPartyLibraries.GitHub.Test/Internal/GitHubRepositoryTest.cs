﻿using System.Net;
using System.Net.Mime;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;
using ThirdPartyLibraries.GitHub.Configuration;

namespace ThirdPartyLibraries.GitHub.Internal;

[TestFixture]
public class GitHubRepositoryTest
{
    private GitHubConfiguration _configuration = null!;
    private MockHttpMessageHandler _mockHttp = null!;
    private GitHubRepository _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _configuration = new GitHubConfiguration();
        _mockHttp = new MockHttpMessageHandler();

        _sut = new GitHubRepository(
            new OptionsWrapper<GitHubConfiguration>(_configuration),
            _mockHttp.ToHttpClient);
    }

    [Test]
    public async Task NoAuthorization()
    {
        _mockHttp
            .When(HttpMethod.Get, DummyResponse.Uri)
            .With(request =>
            {
                request.Headers.Authorization.ShouldBeNull();
                return true;
            })
            .Respond(
                MediaTypeNames.Application.Json,
                DummyResponse.Content.AsStream());

        var actual = await _sut.GetAsJsonAsync(DummyResponse.Uri, DummyJsonSerializerContext.Default.DummyResponse, default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Foo.ShouldBe(1);
    }

    [Test]
    public async Task WithAuthorization()
    {
        _configuration.PersonalAccessToken = "tokenValue";

        _mockHttp
            .When(HttpMethod.Get, DummyResponse.Uri)
            .WithHeaders("Authorization", "Token tokenValue")
            .Respond(
                MediaTypeNames.Application.Json,
                DummyResponse.Content.AsStream());

        var actual = await _sut.GetAsJsonAsync(DummyResponse.Uri, DummyJsonSerializerContext.Default.DummyResponse, default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Foo.ShouldBe(1);
    }

    [Test]
    public async Task NotFound()
    {
        _mockHttp
            .When(HttpMethod.Get, DummyResponse.Uri)
            .Respond(HttpStatusCode.NotFound);

        var actual = await _sut.GetAsJsonAsync(DummyResponse.Uri, DummyJsonSerializerContext.Default.DummyResponse, default).ConfigureAwait(false);

        actual.ShouldBeNull();
    }

    [Test]
    public void Unauthorized()
    {
        _mockHttp
            .When(HttpMethod.Get, DummyResponse.Uri)
            .Respond(
                HttpStatusCode.Unauthorized,
                MediaTypeNames.Application.Json,
                TempFile.OpenResource(GetType(), "GitHubRepositoryTest.Unauthorized.json"));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetAsJsonAsync(DummyResponse.Uri, DummyJsonSerializerContext.Default.DummyResponse, default));

        ex.ShouldNotBeNull();
        ex.Message.ShouldContain("Bad credentials");
    }

    [Test]
    public void ApiRateLimitExceeded()
    {
        var headers = new Dictionary<string, string>
        {
            { "X-RateLimit-Limit", "60" },
            { "X-RateLimit-Remaining", "0" },
            { "X-RateLimit-Reset", "1372700873" }
        };
        var content = new StringContent(
            "{ \"message\": \"some text\", \"documentation_url\": \"some url\" }",
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        _mockHttp
            .When(HttpMethod.Get, DummyResponse.Uri)
            .Respond(HttpStatusCode.Forbidden, headers, content);

        var actual = Assert.ThrowsAsync<ApiRateLimitExceededException>(() => _sut.GetAsJsonAsync(DummyResponse.Uri, DummyJsonSerializerContext.Default.DummyResponse, default));

        Console.WriteLine(actual);

        actual.ShouldNotBeNull();
        actual.Limit.ShouldBe(60);
        actual.Remaining.ShouldBe(0);
        actual.Reset.Date.ShouldBe(new DateTime(2013, 07, 01));
        actual.Message.ShouldContain("some text");
        actual.Message.ShouldContain("some url");
    }
}