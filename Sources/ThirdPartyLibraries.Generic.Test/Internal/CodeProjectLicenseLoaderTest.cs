using System.Net.Mime;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

namespace ThirdPartyLibraries.Generic.Internal;

[TestFixture]
public class CodeProjectLicenseLoaderTest
{
    private MockHttpMessageHandler _mockHttp = null!;
    private CodeProjectLicenseLoader _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _mockHttp = new MockHttpMessageHandler();
        _sut = new CodeProjectLicenseLoader(_mockHttp.ToHttpClient);
    }

    [Test]
    public async Task TryDownloadByUrlAsync()
    {
        var actual = await _sut.TryDownloadAsync(new Uri("http://www.codeproject.com/info/cpol10.aspx"), default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Code.ShouldBe(CodeProjectLicenseLoader.LicenseCode);
    }

    [Test]
    public async Task TryDownloadByCodeAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://www.codeproject.com/info/CPOL.zip")
            .Respond(
                MediaTypeNames.Application.Zip,
                TempFile.OpenResource(GetType(), "CodeProjectLicenseLoaderTest.CPOL.zip"));

        var actual = await _sut.TryDownloadAsync(CodeProjectLicenseLoader.LicenseCode, default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Code.ShouldBe(CodeProjectLicenseLoader.LicenseCode);
        actual.FullName.ShouldNotBeEmpty();
        actual.FileExtension.ShouldBe(".htm");
        actual.HRef.ShouldBe("https://www.codeproject.com/info/cpol10.aspx");
        actual.FileContent.ShouldNotBeNull();
        actual.FileContent.AsText().ShouldContain("<title>The Code Project Open License (CPOL)</title>");
    }
}