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
    public class OpenSourceOrgApiTest
    {
        private MockHttpMessageHandler _mockHttp;
        private OpenSourceOrgApi _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            _mockHttp = new MockHttpMessageHandler();
            _sut = new OpenSourceOrgApi(_mockHttp.ToHttpClient);
        }

        [Test]
        [TestCase("https://api.opensource.org/license/MIT", "MIT")]
        [TestCase("https://opensource.org/licenses/MIT", "MIT")]
        [TestCase("https://opensource.org/licenses/BSD-3", "BSD-3-Clause")]
        [TestCase("https://opensource.org/licenses/BSD-3-clause", "BSD-3-Clause")]
        public async Task ResolveLicenseCode(string url, string expected)
        {
            _mockHttp
                .When(HttpMethod.Get, "https://api.opensource.org/licenses/")
                .Respond(
                    MediaTypeNames.Application.Json,
                    TempFile.OpenResource(GetType(), "OpenSourceOrgApi.Licenses.json"));

            var actual = await _sut.ResolveLicenseCodeAsync(url, CancellationToken.None);

            actual.ShouldBe(expected);
        }

        [Test]
        public async Task ResolveLicenseCodeNotFound()
        {
            _mockHttp
                .When(HttpMethod.Get, "https://api.opensource.org/licenses/")
                .Respond(HttpStatusCode.NotFound);

            var actual = await _sut.ResolveLicenseCodeAsync("https://opensource.org/licenses/MIT", CancellationToken.None);

            actual.ShouldBeNull();
        }
    }
}
