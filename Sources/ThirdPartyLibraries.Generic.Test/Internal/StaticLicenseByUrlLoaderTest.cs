using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Generic.Configuration;

namespace ThirdPartyLibraries.Generic.Internal;

[TestFixture]
public class StaticLicenseByUrlLoaderTest
{
    private List<StaticLicenseByUrl> _configuration = null!;
    private StaticLicenseByUrlLoader _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        var configuration = new StaticLicenseConfiguration
        {
            ByUrl =
            {
                new StaticLicenseByUrl
                {
                    Code = "Apache-2.0",
                    Urls = new[]
                    {
                        "https://www.apache.org/licenses/LICENSE-2.0.html",
                        "https://go.microsoft.com/fwlink/?LinkId=331280"
                    }
                }
            }
        };

        _configuration = configuration.ByUrl;

        _sut = new StaticLicenseByUrlLoader(new OptionsWrapper<StaticLicenseConfiguration>(configuration));
    }

    [Test]
    [TestCase("https://www.apache.org/licenses/LICENSE-2.0.html")]
    [TestCase("http://www.apache.org/licenses/license-2.0.html")]
    [TestCase("https://www.apache.org/licenses/LICENSE-2.0.html?Query")]
    [TestCase("https://go.microsoft.com/fwlink/?LinkId=331280")]
    public async Task DownloadAsync(string url)
    {
        var actual = await _sut.TryDownloadAsync(new Uri(url), default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        
        actual.Code.ShouldBe(_configuration[0].Code);
        actual.HRef.ShouldBe(new Uri(url).ToString());
        actual.FullName.ShouldBeNull();
        actual.FileContent.ShouldBeNull();
    }

    [Test]
    [TestCase("https://www.apache.org/licenses/LICENSE-3.0.html")]
    [TestCase("https://go.microsoft.com/fwlink/")]
    public async Task NotFoundDownloadAsync(string url)
    {
        var actual = await _sut.TryDownloadAsync(new Uri(url), default).ConfigureAwait(false);

        actual.ShouldBeNull();
    }
}