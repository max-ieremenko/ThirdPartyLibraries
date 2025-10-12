using System.Net;
using System.Net.Mime;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;
using ThirdPartyLibraries.Generic.Internal.Domain;

namespace ThirdPartyLibraries.Generic.Internal;

[TestFixture]
public class OpenSourceOrgRepositoryTest
{
    private MockHttpMessageHandler _mockHttp = null!;
    private OpenSourceOrgRepository _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _mockHttp = new MockHttpMessageHandler();
        _sut = new OpenSourceOrgRepository(_mockHttp.ToHttpClient);
    }

    [Test]
    public async Task LoadIndexAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://opensource.org/api/license/")
            .Respond(
                MediaTypeNames.Application.Json,
                TempFile.OpenResource(GetType(), "OpenSourceOrgRepositoryTest.Index.json"));

        await _sut.LoadIndexAsync(default).ConfigureAwait(false);

        _sut.Index.ShouldNotBeNull();

        _sut.Index.TryGetCode("apache-2-0", out var code).ShouldBeTrue();
        code.ShouldBe("Apache-2.0");

        _sut.Index.TryGetCode(new Uri("https://www.apache.org/licenses/license-2.0"), out code).ShouldBeTrue();
        code.ShouldBe("Apache-2.0");

        _sut.Index.TryGetCode("bsd-3-clause", out code).ShouldBeTrue();
        code.ShouldBe("BSD-3-Clause");
    }

    [Test]
    public async Task GetOrLoadIndexNotFoundAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://opensource.org/api/license/")
            .Respond(HttpStatusCode.NotFound);

        await _sut.LoadIndexAsync(default).ConfigureAwait(false);

        _sut.Index.ShouldNotBeNull();

        _sut.Index.TryGetCode("apache-2-0", out _).ShouldBeFalse();
    }

    [Test]
    [TestCase("https://opensource.org/license/apache-2.0")]
    [TestCase("https://opensource.org/license/apache-2-0")]
    [TestCase("https://opensource.org/licenses/apache-2-0")]
    [TestCase("https://www.apache.org/licenses/license-2.0")]
    [TestCase("https://opensource.org/api/licenses/apache-2-0")]
    [TestCase("https://opensource.org/api/license/apache-2-0")]
    [TestCase("https://api.opensource.org/license/apache-2-0")] // deprecated API
    [TestCase("https://api.opensource.org/licenses/apache-2-0")]
    [TestCase("https://api.opensource.org/license/apache-2.0")]
    public void TryFindLicenseCodeByUrl(string url)
    {
        var entry = new OsiApprovedLicense
        {
            Id = "apache-2-0",
            SpdxId = "Apache-2.0",
            LicenseStewardUrl = "https://www.apache.org/licenses/LICENSE-2.0"
        };

        _sut.Index = new OsiLicenseIndex(1, 1);
        _sut.Index.Add(entry.Id, entry.SpdxId);
        _sut.Index.Add(entry.SpdxId, new Uri(entry.LicenseStewardUrl));

        _sut.TryFindLicenseCodeByUrl(new Uri(url), out var actual).ShouldBeTrue();

        actual.ShouldBe("Apache-2.0");
    }

    [Test]
    [TestCase("apache-2.0")]
    [TestCase("apache-2-0")]
    [TestCase("Apache-2.0")]
    public void TryFindLicenseCode(string code)
    {
        var entry = new OsiApprovedLicense
        {
            Id = "apache-2-0",
            SpdxId = "Apache-2.0"
        };

        _sut.Index = new OsiLicenseIndex(1, 0);
        _sut.Index.Add(entry.Id, entry.SpdxId);

        _sut.TryFindLicenseCode(code, out var actual).ShouldBeTrue();

        actual.ShouldBe("Apache-2.0");
    }
}