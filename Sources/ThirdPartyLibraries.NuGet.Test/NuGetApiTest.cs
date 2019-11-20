using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

namespace ThirdPartyLibraries.NuGet
{
    [TestFixture]
    public class NuGetApiTest
    {
        private NuGetApi _sut;
        private MockHttpMessageHandler _mockHttp;

        [SetUp]
        public void BeforeEachTest()
        {
            _mockHttp = new MockHttpMessageHandler();

            _sut = new NuGetApi(_mockHttp.ToHttpClient);
        }

        [Test]
        public async Task LoadSpecNewtonsoftJson()
        {
            var package = new NuGetPackageId("Newtonsoft.Json", "12.0.2");

            _mockHttp
                .When(HttpMethod.Get, NuGetApi.Host + "/v3-flatcontainer/Newtonsoft.Json/12.0.2/Newtonsoft.Json.nuspec")
                .Respond(
                    MediaTypeNames.Application.Xml,
                    TempFile.OpenResource(GetType(), "NuGetApiTest.Newtonsoft.Json.12.0.2.nuspec.xml"));

            var specContent = await _sut.LoadSpecAsync(package, false, CancellationToken.None);
            var spec = _sut.ParseSpec(new MemoryStream(specContent));

            spec.ShouldNotBeNull();
            spec.Id.ShouldBe("Newtonsoft.Json");
            spec.Version.ShouldBe("12.0.2");
            spec.PackageHRef.ShouldBe("https://www.nuget.org/packages/Newtonsoft.Json/12.0.2");
            spec.Description.ShouldBe("Json.NET is a popular high-performance JSON framework for .NET");
            spec.Authors.ShouldBe("James Newton-King");
            spec.Copyright.ShouldBe("Copyright © James Newton-King 2008");

            spec.License.ShouldNotBeNull();
            spec.License.Type.ShouldBe("expression");
            spec.License.Value.ShouldBe("MIT");
            spec.LicenseUrl.ShouldBe("https://licenses.nuget.org/MIT");
            
            spec.ProjectUrl.ShouldBe("https://www.newtonsoft.com/json");
            
            spec.Repository.ShouldNotBeNull();
            spec.Repository.Type.ShouldBe("git");
            spec.Repository.Url.ShouldBe("https://github.com/JamesNK/Newtonsoft.Json");
        }

        [Test]
        public async Task LoadSpecStyleCopAnalyzers()
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");

            _mockHttp
                .When(HttpMethod.Get, NuGetApi.Host + "/v3-flatcontainer/StyleCop.Analyzers/1.1.118/StyleCop.Analyzers.nuspec")
                .Respond(
                    MediaTypeNames.Application.Xml,
                    TempFile.OpenResource(GetType(), "NuGetApiTest.StyleCop.Analyzers.1.1.118.nuspec.xml"));

            var specContent = await _sut.LoadSpecAsync(package, false, CancellationToken.None);
            var spec = _sut.ParseSpec(new MemoryStream(specContent));

            spec.ShouldNotBeNull();
            spec.Id.ShouldBe("StyleCop.Analyzers");
            spec.Version.ShouldBe("1.1.118");
            spec.PackageHRef.ShouldBe("https://www.nuget.org/packages/StyleCop.Analyzers/1.1.118");
            spec.Description.ShouldBe("An implementation of StyleCop's rules using Roslyn analyzers and code fixes");
            spec.Authors.ShouldBe("Sam Harwell et. al.");
            spec.Copyright.ShouldBe("Copyright 2015 Tunnel Vision Laboratories, LLC");

            spec.License.ShouldNotBeNull();
            spec.License.Type.ShouldBe("expression");
            spec.License.Value.ShouldBe("Apache-2.0");
            spec.LicenseUrl.ShouldBe("https://licenses.nuget.org/Apache-2.0");

            spec.Repository.ShouldBeNull();

            spec.ProjectUrl.ShouldBe("https://github.com/DotNetAnalyzers/StyleCopAnalyzers");
        }

        // version in the spec 1.0 must converted to 1.0.0
        [Test]
        public async Task LoadSpecOwin()
        {
            var package = new NuGetPackageId("Owin", "1.0.0");

            _mockHttp
                .When(HttpMethod.Get, NuGetApi.Host + "/v3-flatcontainer/Owin/1.0.0/Owin.nuspec")
                .Respond(
                    MediaTypeNames.Application.Xml,
                    TempFile.OpenResource(GetType(), "NuGetApiTest.Owin.1.0.nuspec.xml"));

            var specContent = await _sut.LoadSpecAsync(package, false, CancellationToken.None);
            var spec = _sut.ParseSpec(new MemoryStream(specContent));

            spec.ShouldNotBeNull();
            spec.Id.ShouldBe("Owin");
            spec.Version.ShouldBe("1.0.0");
            spec.Description.ShouldBe("OWIN IAppBuilder startup interface");
            spec.Authors.ShouldBe("OWIN startup components contributors");
            spec.Copyright.ShouldBeNull();

            spec.LicenseUrl.ShouldBe("https://github.com/owin-contrib/owin-hosting/blob/master/LICENSE.txt");
            spec.ProjectUrl.ShouldBe("https://github.com/owin-contrib/owin-hosting/");
            
            spec.Repository.ShouldBeNull();
        }

        [Test]
        public async Task LoadSpecStyleCopAnalyzersFromLocalCache()
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");
            var specContent = await _sut.LoadSpecAsync(package, true, CancellationToken.None);
            var spec = _sut.ParseSpec(new MemoryStream(specContent));

            spec.ShouldNotBeNull();
            spec.Id.ShouldBe("StyleCop.Analyzers");
            spec.Version.ShouldBe("1.1.118");
            spec.PackageHRef.ShouldBe("https://www.nuget.org/packages/StyleCop.Analyzers/1.1.118");
        }

        [Test]
        public async Task LoadStyleCopAnalyzersLicenseContentFromLocalCache()
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");
            var content = await _sut.LoadFileContentAsync(package, "LICENSE", true, CancellationToken.None);
            
            content.AsText().ShouldContain("Copyright (c) Tunnel Vision Laboratories");
        }

        [Test]
        public async Task FindLicenseFileStyleCopAnalyzersInLocalCache()
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");
            var file = await _sut.TryFindLicenseFileAsync(package, true, CancellationToken.None);

            file?.Name.ShouldBe("LICENSE");
            file?.Content.AsText().ShouldContain("Copyright (c) Tunnel Vision Laboratories");
        }

        [Test]
        public async Task LoadSpecNotFound()
        {
            var package = new NuGetPackageId("Newtonsoft.Json", "12.0.2");

            _mockHttp
                .When(HttpMethod.Get, NuGetApi.Host + "/v3-flatcontainer/Newtonsoft.Json/12.0.2/Newtonsoft.Json.nuspec")
                .Respond(HttpStatusCode.NotFound);

            var spec = await _sut.LoadSpecAsync(package, false, CancellationToken.None);

            spec.ShouldBeNull();
        }

        [Test]
        public async Task ResolveLicenseCodeMIT()
        {
            const string Url = "https://licenses.nuget.org/MIT";

            _mockHttp
                .When(HttpMethod.Get, Url)
                .Respond(
                    MediaTypeNames.Text.Html,
                    TempFile.OpenResource(GetType(), "NuGetApiTest.MIT.html"));

            var actual = await _sut.ResolveLicenseCodeAsync(Url, CancellationToken.None);

            actual.ShouldBe("MIT");
        }

        [Test]
        public async Task ResolveLicenseCodeExpression()
        {
            const string Url = "https://licenses.nuget.org/(LGPL-2.0-only%20WITH%20FLTK-exception%20OR%20Apache-2.0+)";

            _mockHttp
                .When(HttpMethod.Get, Url)
                .Respond(
                    MediaTypeNames.Text.Html,
                    TempFile.OpenResource(GetType(), "NuGetApiTest.Mixed.html"));

            var actual = await _sut.ResolveLicenseCodeAsync(Url, CancellationToken.None);

            actual.ShouldBe("(LGPL-2.0-only WITH FLTK-exception OR Apache-2.0 )");
        }

        [Test]
        public async Task ResolveLicenseCodeNotFound()
        {
            const string Url = "https://licenses.nuget.org/MIT";

            _mockHttp
                .When(HttpMethod.Get, Url)
                .Respond(HttpStatusCode.NotFound);

            var actual = await _sut.ResolveLicenseCodeAsync(Url, CancellationToken.None);

            actual.ShouldBeNull();
        }

        [Test]
        [TestCase("LICENSE", "Copyright (c) Tunnel Vision Laboratories")]
        [TestCase("LICENSE.txt", null)]
        [TestCase("tools/install.ps1", "param($installPath, $toolsPath, $package, $project)")]
        [TestCase("tools\\install.ps1", "param($installPath, $toolsPath, $package, $project)")]
        public async Task LoadFileContentStyleCopAnalyzers(string fileName, string expected)
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");

            _mockHttp
                .When(HttpMethod.Get, NuGetApi.Host + "/v3-flatcontainer/stylecop.analyzers/1.1.118/stylecop.analyzers.1.1.118.nupkg")
                .Respond(
                    MediaTypeNames.Application.Octet,
                    TempFile.OpenResource(GetType(), "NuGetApiTest.StyleCop.Analyzers.1.1.118.nupkg"));

            var content = await _sut.LoadFileContentAsync(package, fileName, false, CancellationToken.None);

            if (expected == null)
            {
                content.ShouldBeNull();
            }
            else
            {
                content.AsText().ShouldContain(expected);
            }
        }

        [Test]
        public async Task FindLicenseFileStyleCopAnalyzers()
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");

            _mockHttp
                .When(HttpMethod.Get, NuGetApi.Host + "/v3-flatcontainer/StyleCop.Analyzers/1.1.118/StyleCop.Analyzers.nupkg")
                .Respond(
                    MediaTypeNames.Application.Octet,
                    TempFile.OpenResource(GetType(), "NuGetApiTest.StyleCop.Analyzers.1.1.118.nupkg"));

            var file = await _sut.TryFindLicenseFileAsync(package, false, CancellationToken.None);

            file?.Name.ShouldBe("LICENSE");
            file?.Content.AsText().ShouldContain("Copyright (c) Tunnel Vision Laboratories");
        }

        [Test]
        public async Task LoadPackageStyleCopAnalyzersFromWeb()
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");

            _mockHttp
                .When(HttpMethod.Get, NuGetApi.Host + "/v3-flatcontainer/stylecop.analyzers/1.1.118/stylecop.analyzers.1.1.118.nupkg")
                .Respond(
                    MediaTypeNames.Application.Octet,
                    TempFile.OpenResource(GetType(), "NuGetApiTest.StyleCop.Analyzers.1.1.118.nupkg"));

            var file = await _sut.LoadPackageAsync(package, false, CancellationToken.None);

            file.ShouldNotBeNull();
        }

        [Test]
        public async Task LoadPackageStyleCopAnalyzersFromLocalCache()
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");

            var file = await _sut.LoadPackageAsync(package, true, CancellationToken.None);

            file.ShouldNotBeNull();
        }

        [Test]
        [TestCase("https://licenses.nuget.org/MIT", "MIT")]
        [TestCase("https://licenses.nuget.org/(MIT)", "(MIT)")]
        [TestCase("https://licenses.nuget.org/(LGPL-2.0-only%20WITH%20FLTK-exception%20OR%20Apache-2.0+)", "(LGPL-2.0-only WITH FLTK-exception OR Apache-2.0 )")]
        [TestCase("https://licenses.nuget.org/", null)]
        [TestCase("https://licenses.nuget.org", null)]
        public void ExtractLicenseCode(string url, string expected)
        {
            var actual = NuGetApi.ExtractLicenseCode(url);
            actual.ShouldBe(expected);
        }
    }
}
