using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

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
            .When(HttpMethod.Get, "https://api.opensource.org/licenses/")
            .Respond(
                MediaTypeNames.Application.Json,
                TempFile.OpenResource(GetType(), "OpenSourceOrgRepositoryTest.Index.json"));

        await _sut.LoadIndexAsync(default).ConfigureAwait(false);

        _sut.Index.ShouldNotBeNull();

        _sut.Index.TryGetEntry("AAL", out var aal).ShouldBeTrue();
        aal.ShouldNotBeNull();

        aal.FullName.ShouldBe("Attribution Assurance License");
        aal.Urls.ShouldBe(new[] { new Uri("https://opensource.org/licenses/AAL") });
        aal.DownloadUrl.ShouldBe(new Uri("https://opensource.org/licenses/AAL"));

        _sut.Index.TryGetEntry("AAL-test", out var aalTest).ShouldBeTrue();
        aalTest.ShouldBe(aal);

        _sut.Index.TryGetEntry("Apache-2.0", out var apache).ShouldBeTrue();
        apache.ShouldNotBeNull();

        apache.FullName.ShouldBe("Apache License, Version 2.0");
        apache.Urls.ShouldBe(
            new[]
            {
                new Uri("https://tldrlegal.com/license/apache-license-2.0-%28apache-2.0%29"),
                new Uri("https://en.wikipedia.org/wiki/Apache_License"),
                new Uri("https://opensource.org/licenses/Apache-2.0"),
                new Uri("https://www.apache.org/licenses/LICENSE-2.0"),
                new Uri("https://www.gnu.org/licenses/apache-2.0.txt")
            },
            ignoreOrder: true);
        apache.DownloadUrl.ShouldBe(new Uri("https://www.gnu.org/licenses/apache-2.0.txt"));
    }

    [Test]
    public async Task GetOrLoadIndexNotFoundAsync()
    {
        _mockHttp
            .When(HttpMethod.Get, "https://api.opensource.org/licenses/")
            .Respond(HttpStatusCode.NotFound);

        await _sut.LoadIndexAsync(default).ConfigureAwait(false);

        _sut.Index.ShouldNotBeNull();
    }

    [Test]
    public void TryFindLicenseCodeByUrlFoundByCodeFromUrlPath()
    {
        var entry = new OpenSourceOrgLicenseEntry("BSD-3-clause", "dummy");

        _sut.Index = new OpenSourceOrgIndex(1);
        _sut.Index.Add(entry);
        _sut.Index.TryAdd("BSD-3", entry);

        _sut.TryFindLicenseCodeByUrl(new Uri("https://opensource.org/licenses/BSD-3"), out var actual).ShouldBeTrue();

        actual.ShouldBe("BSD-3-clause");
    }

    [Test]
    public void TryFindLicenseCodeByUrlFoundByUrl()
    {
        var entry = new OpenSourceOrgLicenseEntry("Apache-2.0", "dummy")
        {
            Urls = { new("https://www.gnu.org/licenses/apache-2.0.txt") }
        };

        _sut.Index = new OpenSourceOrgIndex(1);
        _sut.Index.Add(entry);

        _sut.TryFindLicenseCodeByUrl(new Uri("https://www.gnu.org/licenses/apache-2.0.txt"), out var actual).ShouldBeTrue();

        actual.ShouldBe("Apache-2.0");
    }

    [Test]
    public async Task TryDownloadByCodeAsync()
    {
        var entry = new OpenSourceOrgLicenseEntry("GPL-2.0", "GNU General Public License, Version 2.0")
        {
            DownloadUrl = new Uri("https://www.gnu.org/licenses/old-licenses/gpl-2.0.txt")
        };

        _sut.Index = new OpenSourceOrgIndex(1);
        _sut.Index.Add(entry);

        _mockHttp
            .When(HttpMethod.Get, "https://www.gnu.org/licenses/old-licenses/gpl-2.0.txt")
            .Respond(
                MediaTypeNames.Text.Plain,
                TempFile.OpenResource(GetType(), "OpenSourceOrgRepositoryTest.gpl-2.0.txt"));

        var actual = await _sut.TryDownloadByCodeAsync("gpl-2.0", default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Code.ShouldBe(entry.Code);
        actual.FullName.ShouldBe(entry.FullName);
        actual.HRef.ShouldBe(entry.DownloadUrl.ToString());
        actual.FileExtension.ShouldBe(".txt");
        actual.FileContent.ShouldNotBeEmpty();
        actual.FileContent.AsText().ShouldContain("GNU GENERAL PUBLIC LICENSE");
    }
}