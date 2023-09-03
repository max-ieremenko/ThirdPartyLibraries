using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

namespace ThirdPartyLibraries.Generic.Internal;

[TestFixture]
public class SpdxOrgRepositoryTest
{
    private MockHttpMessageHandler _mockHttp = null!;
    private SpdxOrgRepository _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _mockHttp = new MockHttpMessageHandler();
        _sut = new SpdxOrgRepository(_mockHttp.ToHttpClient);
    }

    [Test]
    [TestCase("https://spdx.org/licenses/MIT.html", "MIT")]
    [TestCase("https://spdx.org/licenses/MIT", "MIT")]
    [TestCase("https://spdx.org/licenses/MIT.txt", "MIT")]
    [TestCase("https://spdx.org/licenses/MIT.json", "MIT")]
    [TestCase("https://spdx.org/licenses/.json", null)]
    public void TryParseLicenseCode(string url, string? expected)
    {
        _sut.TryParseLicenseCode(new Uri(url), out var actual).ShouldBe(expected != null);

        actual.ShouldBe(expected);
    }

    [Test]
    public async Task DownloadByCodeAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://spdx.org/licenses/MIT.json")
            .Respond(
                MediaTypeNames.Application.Json,
                TempFile.OpenResource(GetType(), "SpdxOrgRepositoryTest.License.MIT.json"));

        var actual = await _sut.TryDownloadByCodeAsync("MIT", default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Code.ShouldBe("MIT");
        actual.FullName.ShouldBe("MIT License");
        actual.FileExtension = ".txt";
        actual.FileContent.ShouldNotBeNull();
        actual.FileContent.AsText().ShouldContain("MIT License Copyright");
        actual.HRef.ShouldBe("https://spdx.org/licenses/MIT");
    }

    [Test]
    public async Task NotFoundDownloadByCodeAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://spdx.org/licenses/mit.json")
            .Respond(HttpStatusCode.NotFound);

        var actual = await _sut.TryDownloadByCodeAsync("mit", default).ConfigureAwait(false);

        actual.ShouldBeNull();
    }
}