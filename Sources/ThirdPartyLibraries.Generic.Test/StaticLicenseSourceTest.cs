using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

namespace ThirdPartyLibraries.Generic
{
    [TestFixture]
    public class StaticLicenseSourceTest
    {
        private MockHttpMessageHandler _mockHttp;
        private StaticLicenseConfiguration _configuration;
        private StaticLicenseSource _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            _mockHttp = new MockHttpMessageHandler();
            _configuration = new StaticLicenseConfiguration();

            using (var file = TempFile.FromResource(GetType(), "StaticLicenseSourceTest.appsettings.json"))
            {
                var root = new ConfigurationBuilder()
                    .AddJsonFile(file.Location, false, false)
                    .Build();

                root.GetSection(StaticLicenseConfiguration.SectionName).Bind(_configuration);
            }

            _sut = new StaticLicenseSource(_configuration, _mockHttp.ToHttpClient);
        }

        [Test]
        public void ConfigurationTest()
        {
            _configuration.ByCode.Count.ShouldBe(2);
            _configuration.ByCode[0].Code.ShouldBe("ms-net-library");
            _configuration.ByCode[0].FullName.ShouldBe("MICROSOFT .NET LIBRARY");
            _configuration.ByCode[0].DownloadUrl.ShouldBe("https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm");

            _configuration.ByUrl.Count.ShouldBe(4);
            _configuration.ByUrl[0].Code.ShouldBe("MIT");
            _configuration.ByUrl[0].Urls.Length.ShouldBe(1);
            _configuration.ByUrl[0].Urls[0].ShouldBe("https://github.com/dotnet/corefx/blob/master/LICENSE.TXT");
        }

        [Test]
        [TestCase("http://www.apache.org/licenses/LICENSE-2.0.html", "Apache-2.0")]
        [TestCase("https://www.microsoft.com/web/webpi/eula/SysClrTypes_SQLServer.htm", "ms-clr-types-sql-server-2012")]
        [TestCase("http://go.microsoft.com/fwlink/?LinkId=329770", "ms-net-library")]
        [TestCase("https://go.microsoft.com/fwlink/?LinkId=329770", "ms-net-library")]
        [TestCase("https://unknown.host/fwlink/?LinkId=329770", null)]
        public async Task ResolveLicenseCode(string licenseUrl, string expected)
        {
            var actual = await _sut.ResolveLicenseCodeAsync(licenseUrl, CancellationToken.None);

            actual.ShouldBe(expected);
        }

        [Test]
        public async Task DownloadLicenseByCode()
        {
            _mockHttp
                .When(HttpMethod.Get, "https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm")
                .Respond(
                    MediaTypeNames.Text.Html,
                    TempFile.OpenResource(GetType(), "StaticLicenseSourceTest.net_library_eula_enu.htm"));

            var actual = await _sut.DownloadLicenseByCodeAsync("ms-net-library", CancellationToken.None);

            actual.ShouldNotBeNull();
            actual.Code.ShouldBe("ms-net-library");
            actual.FullName.ShouldBe("MICROSOFT .NET LIBRARY");
            actual.FileHRef.ShouldBe("https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm");
            actual.FileName.ShouldBe("license.html");
            actual.FileContent.AsText().ShouldContain("MICROSOFT SOFTWARE LICENSE");
        }
    }
}
