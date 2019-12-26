using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm
{
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

            var content = await _sut.DownloadPackageAsync(package, CancellationToken.None);

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
                jsonContent = await _sut.ExtractPackageJsonAsync(await content.ToArrayAsync(CancellationToken.None), CancellationToken.None);
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
        public async Task LoadFileContent()
        {
            byte[] file;
            using (var package = TempFile.OpenResource(GetType(), "NpmApiTest.TypesAngular.1.6.56.tgz"))
            {
                file = await _sut.LoadFileContentAsync(await package.ToArrayAsync(CancellationToken.None), "LICENSE", CancellationToken.None);
            }

            file.ShouldNotBeNull();

            string fileContent;
            using (var reader = new StreamReader(new MemoryStream(file)))
            {
                fileContent = await reader.ReadToEndAsync();
            }

            fileContent.ShouldContain("Copyright (c) Microsoft Corporation");
        }

        [Test]
        public async Task TryFindLicenseFile()
        {
            NpmPackageFile? file;
            using (var package = TempFile.OpenResource(GetType(), "NpmApiTest.TypesAngular.1.6.56.tgz"))
            {
                file = await _sut.TryFindLicenseFileAsync(await package.ToArrayAsync(CancellationToken.None), CancellationToken.None);
            }

            file.ShouldNotBeNull();

            file.Value.Name.ShouldBe("LICENSE");

            string fileContent;
            using (var reader = new StreamReader(new MemoryStream(file.Value.Content)))
            {
                fileContent = await reader.ReadToEndAsync();
            }

            fileContent.ShouldContain("Copyright (c) Microsoft Corporation");
        }
    }
}
