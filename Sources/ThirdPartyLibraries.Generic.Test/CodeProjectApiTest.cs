using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

namespace ThirdPartyLibraries.Generic;

[TestFixture]
public class CodeProjectApiTest
{
    private MockHttpMessageHandler _mockHttp;
    private CodeProjectApi _sut;

    [SetUp]
    public void BeforeEachTest()
    {
        _mockHttp = new MockHttpMessageHandler();
        _sut = new CodeProjectApi(_mockHttp.ToHttpClient);
    }

    [Test]
    [TestCase("http://www.codeproject.com/info/cpol10.aspx")]
    [TestCase("https://www.codeproject.com/info/cpol10.aspx")]
    public async Task ResolveLicenseCode(string url)
    {
        var actual = await _sut.ResolveLicenseCodeAsync(url, CancellationToken.None).ConfigureAwait(false);

        actual.ShouldBe(CodeProjectApi.LicenseCode);
    }

    [Test]
    public async Task DownloadLicenseByCode()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://www.codeproject.com/info/CPOL.zip")
            .Respond(
                MediaTypeNames.Application.Zip,
                TempFile.OpenResource(GetType(), "CodeProjectApiTest.CPOL.zip"));

        var actual = await _sut.DownloadLicenseByCodeAsync(CodeProjectApi.LicenseCode, CancellationToken.None).ConfigureAwait(false);

        actual.Code.ShouldBe(CodeProjectApi.LicenseCode);
        actual.FullName.ShouldNotBeNull();
        actual.FileName.ShouldBe("CPOL.htm");
        actual.FileHRef.ShouldBe("https://www.codeproject.com/info/cpol10.aspx");
        actual.FileContent.AsText().ShouldContain("<title>The Code Project Open License (CPOL)</title>");
    }
}