using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.NuGet.Internal;

[TestFixture]
public class NuGetLicenseByUrlLoaderTest
{
    private MockHttpMessageHandler _mockHttp = null!;
    private NuGetLicenseByUrlLoader _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _mockHttp = new MockHttpMessageHandler();
        _sut = new NuGetLicenseByUrlLoader(_mockHttp.ToHttpClient);
    }

    [Test]
    public async Task DownloadMitLicenseAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://licenses.nuget.org/(MIT)")
            .Respond(
                MediaTypeNames.Text.Html,
                TempFile.OpenResource(GetType(), "NuGetLicenseByUrlLoaderTest.MIT.html"));

        var actual = await _sut.TryDownloadAsync(new Uri("https://licenses.nuget.org/(MIT)"), default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Code.ShouldBe("MIT");
        actual.Source.ShouldBe(LicenseSpecSource.Shared);
        actual.FullName.ShouldBeNull();
        actual.FileExtension.ShouldBeNull();
        actual.HRef.ShouldBe("https://licenses.nuget.org/(MIT)");
    }

    [Test]
    public async Task DownloadMixedLicenseAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://licenses.nuget.org/(LGPL-2.0-only%20WITH%20FLTK-exception%20OR%20Apache-2.0+)")
            .Respond(
                MediaTypeNames.Text.Html,
                TempFile.OpenResource(GetType(), "NuGetLicenseByUrlLoaderTest.MIT.html"));

        var actual = await _sut.TryDownloadAsync(new Uri("https://licenses.nuget.org/(LGPL-2.0-only%20WITH%20FLTK-exception%20OR%20Apache-2.0+)"), default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Code.ShouldBe("(LGPL-2.0-only WITH FLTK-exception OR Apache-2.0 )");
        actual.Source.ShouldBe(LicenseSpecSource.Shared);
        actual.FullName.ShouldBeNull();
        actual.FileExtension.ShouldBeNull();
        actual.HRef.ShouldBe("https://licenses.nuget.org/(LGPL-2.0-only WITH FLTK-exception OR Apache-2.0+)");
    }

    [Test]
    public async Task DownloadNotFoundAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://licenses.nuget.org/(MIT)")
            .Respond(HttpStatusCode.NotFound);

        var actual = await _sut.TryDownloadAsync(new Uri("https://licenses.nuget.org/(MIT)"), default).ConfigureAwait(false);

        actual.ShouldBeNull();
    }
}