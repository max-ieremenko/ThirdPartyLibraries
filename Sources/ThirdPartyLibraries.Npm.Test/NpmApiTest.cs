using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm;

[TestFixture]
public class NpmApiTest
{
    private NpmApi _sut;
    private MockHttpMessageHandler _mockHttp;

    [SetUp]
    public void BeforeEachTest()
    {
        _mockHttp = new MockHttpMessageHandler();

        _sut = new NpmApi(_mockHttp.ToHttpClient);
    }

    [Test]
    public async Task DownloadPackage()
    {
        var package = new NpmPackageId("@types/angular", "1.6.55");

        _mockHttp
            .When(HttpMethod.Get, NpmApi.Host + "/@types%2Fangular")
            .Respond(
                MediaTypeNames.Application.Json,
                TempFile.OpenResource(GetType(), "NpmApiTest.TypesAngular.get.json"));

        _mockHttp
            .When(HttpMethod.Get, "https://registry.npmjs.org/@types/angular/-/angular-1.6.55.tgz")
            .Respond(
                MediaTypeNames.Application.Octet,
                TempFile.OpenResource(GetType(), "NpmApiTest.TypesAngular.1.6.56.tgz"));

        var content = await _sut.DownloadPackageAsync(package, CancellationToken.None).ConfigureAwait(false);

        content.ShouldNotBeNull();
        content.Value.Name.ShouldBe("angular-1.6.55.tgz");
        content.Value.Content.Length.ShouldBe(32132);
    }

    [Test]
    public async Task ExtractPackageJson()
    {
        byte[] jsonContent;
        using (var content = TempFile.OpenResource(GetType(), "NpmApiTest.TypesAngular.1.6.56.tgz"))
        {
            jsonContent = _sut.ExtractPackageJson(await content.ToArrayAsync(CancellationToken.None).ConfigureAwait(false));
        }

        var json = _sut.ParsePackageJson(jsonContent);

        json.Name.ShouldBe("@types/angular");
        json.Version.ShouldBe("1.6.56");
        json.PackageHRef.ShouldBe("https://www.npmjs.com/package/@types/angular/v/1.6.56");
        json.HomePage.ShouldBeNull();
        json.Description.ShouldBe("TypeScript definitions for Angular JS");
        json.Authors.ShouldBe("Diego Vilar, Georgii Dolzhykov, Caleb St-Denis, Leonard Thieu, Steffen Kowalski");
        json.License.Type.ShouldBe("expression");
        json.License.Value.ShouldBe("MIT");
        json.Repository.Type.ShouldBe("git");
        json.Repository.Url.ShouldBe("https://github.com/DefinitelyTyped/DefinitelyTyped.git");
    }

    [Test]
    [TestCase("LICENSE", "Copyright (c) Microsoft Corporation")]
    [TestCase("license", "Copyright (c) Microsoft Corporation")]
    [TestCase("license.txt", null)]
    public async Task LoadFileContent(string fileName, string expected)
    {
        byte[] file;
        using (var package = TempFile.OpenResource(GetType(), "NpmApiTest.TypesAngular.1.6.56.tgz"))
        {
            file = _sut.LoadFileContent(await package.ToArrayAsync(CancellationToken.None).ConfigureAwait(false), fileName);
        }

        if (expected == null)
        {
            file.ShouldBeNull();
        }
        else
        {
            file.ShouldNotBeNull();
            file.AsText().ShouldContain(expected);
        }
    }

    [Test]
    [TestCase("^license$", "LICENSE")]
    [TestCase("lic", "LICENSE")]
    [TestCase("i", "index.d.ts", "jqlite.d.ts", "LICENSE")]
    public async Task FindFiles(string searchPattern, params string[] expected)
    {
        string[] actual;
        using (var package = TempFile.OpenResource(GetType(), "NpmApiTest.TypesAngular.1.6.56.tgz"))
        {
            actual = _sut.FindFiles(await package.ToArrayAsync(CancellationToken.None).ConfigureAwait(false), searchPattern);
        }

        actual.ShouldBe(expected, ignoreOrder: true);
    }

    [Test]
    public void ResolveNpmRoot()
    {
        var actual = _sut.ResolveNpmRoot();

        Console.WriteLine(actual);
        actual.ShouldNotBeNull();

        if (!Directory.Exists(actual))
        {
            Path.GetDirectoryName(actual).ShouldNotBeNullOrWhiteSpace();
            DirectoryAssert.Exists(Path.GetDirectoryName(actual));
        }
    }
}