using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

namespace ThirdPartyLibraries.Generic
{
    [TestFixture]
    public class SpdxOrgApiTest
    {
        private MockHttpMessageHandler _mockHttp;
        private SpdxOrgApi _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            _mockHttp = new MockHttpMessageHandler();
            _sut = new SpdxOrgApi(_mockHttp.ToHttpClient);
        }

        [Test]
        [TestCase("https://spdx.org/licenses/MIT.html")]
        [TestCase("https://spdx.org/licenses/MIT")]
        [TestCase("https://spdx.org/licenses/MIT.txt")]
        [TestCase("https://spdx.org/licenses/MIT.json")]
        public async Task ResolveLicenseCodeMIT(string url)
        {
            _mockHttp
                .When(HttpMethod.Get, "https://spdx.org/licenses/MIT.json")
                .Respond(
                    MediaTypeNames.Application.Json,
                    TempFile.OpenResource(GetType(), "SpdxOrgApiTest.License.MIT.json"));

            var actual = await _sut.ResolveLicenseCodeAsync(url, CancellationToken.None);

            actual.ShouldBe("MIT");
        }

        [Test]
        public async Task ResolveLicenseCodeNotFound()
        {
            _mockHttp
                .When(HttpMethod.Get, "https://spdx.org/licenses/MIT.json")
                .Respond(HttpStatusCode.NotFound);

            var actual = await _sut.ResolveLicenseCodeAsync("https://spdx.org/licenses/MIT.json", CancellationToken.None);

            actual.ShouldBeNull();
        }

        [Test]
        public async Task DownloadLicenseByCodeMIT()
        {
            _mockHttp
                .When(HttpMethod.Get, "https://spdx.org/licenses/MIT.json")
                .Respond(
                    MediaTypeNames.Application.Json,
                    TempFile.OpenResource(GetType(), "SpdxOrgApiTest.License.MIT.json"));

            var actual = await _sut.DownloadLicenseByCodeAsync("MIT", CancellationToken.None);

            actual.ShouldNotBeNull();
            actual.Code.ShouldBe("MIT");
            actual.FullName.ShouldBe("MIT License");
            actual.FileName = "license.txt";
            actual.FileContent.AsText().ShouldContain("MIT License Copyright");
            actual.FileHRef.ShouldBe("https://spdx.org/licenses/MIT");
        }
    }
}
