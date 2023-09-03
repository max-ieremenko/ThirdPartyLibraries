using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

namespace ThirdPartyLibraries.Npm.Internal;

[TestFixture]
public class NpmRegistryTest
{
    private NpmRegistry _sut = null!;
    private MockHttpMessageHandler _mockHttp = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _mockHttp = new MockHttpMessageHandler();

        _sut = new NpmRegistry(_mockHttp.ToHttpClient);
    }

    [Test]
    public async Task DownloadPackage()
    {
        _mockHttp
            .When(HttpMethod.Get, NpmRegistry.Host + "/%40types%2Fangular")
            .Respond(
                MediaTypeNames.Application.Json,
                TempFile.OpenResource(GetType(), "NpmRegistryTest.TypesAngular.get.json"));

        _mockHttp
            .When(HttpMethod.Get, "https://registry.npmjs.org/@types/angular/-/angular-1.6.55.tgz")
            .Respond(
                MediaTypeNames.Application.Octet,
                TempFile.OpenResource(GetType(), "NpmRegistryTest.TypesAngular.1.6.56.tgz"));

        var content = await _sut.DownloadPackageAsync("@types/angular", "1.6.55", CancellationToken.None).ConfigureAwait(false);

        content.ShouldNotBeNull();
        content.Length.ShouldBe(32132);
    }
}