using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;
using ThirdPartyLibraries.Generic.Configuration;

namespace ThirdPartyLibraries.Generic.Internal;

[TestFixture]
public class StaticLicenseByCodeLoaderTest
{
    private List<StaticLicenseByCode> _configuration = null!;
    private MockHttpMessageHandler _mockHttp = null!;
    private StaticLicenseByCodeLoader _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _mockHttp = new MockHttpMessageHandler();

        var configuration = new StaticLicenseConfiguration
        {
            ByCode =
            {
                new StaticLicenseByCode
                {
                    Code = "ms-net-library",
                    FullName = "MICROSOFT .NET LIBRARY",
                    DownloadUrl = "https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm"
                }
            }
        };

        _configuration = configuration.ByCode;

        _sut = new StaticLicenseByCodeLoader(
            new OptionsWrapper<StaticLicenseConfiguration>(configuration),
            _mockHttp.ToHttpClient);
    }

    [Test]
    public async Task DownloadAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm")
            .Respond(
                MediaTypeNames.Text.Html,
                TempFile.OpenResource(GetType(), "StaticLicenseByCodeLoaderTest.net_library_eula_enu.htm"));

        var actual = await _sut.TryDownloadAsync("ms-net-library", default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Code.ShouldBe(_configuration[0].Code);
        actual.FullName.ShouldBe(_configuration[0].FullName);
        actual.FileExtension.ShouldBe(".html");
        actual.FileContent.ShouldNotBeNull();
        actual.FileContent.AsText().ShouldContain("MICROSOFT SOFTWARE LICENSE");
    }

    [Test]
    public async Task NotFoundDownloadAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm")
            .Respond(HttpStatusCode.NotFound);

        var actual = await _sut.TryDownloadAsync("ms-net-library", default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Code.ShouldBe(_configuration[0].Code);
        actual.FullName.ShouldBe(_configuration[0].FullName);
        actual.FileExtension.ShouldBeNull();
        actual.FileContent.ShouldBeNull();
    }

    [Test]
    public async Task ConfigurationNotFoundDownloadAsync()
    {
        var actual = await _sut.TryDownloadAsync("unknown", default).ConfigureAwait(false);

        actual.ShouldBeNull();
    }
}