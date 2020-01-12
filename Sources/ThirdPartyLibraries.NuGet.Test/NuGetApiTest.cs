using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;
using ThirdPartyLibraries.Shared;

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
        public async Task ExtractSpecStyleCopAnalyzers()
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");

            byte[] specContent;
            using (var content = TempFile.OpenResource(GetType(), "NuGetApiTest.StyleCop.Analyzers.1.1.118.nupkg"))
            {
                specContent = await _sut.ExtractSpecAsync(package, await content.ToArrayAsync(CancellationToken.None), CancellationToken.None);
            }

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

        [Test]
        public void ParseSpecNewtonsoftJson()
        {
            NuGetSpec spec;
            using (var content = TempFile.OpenResource(GetType(), "NuGetApiTest.Newtonsoft.Json.12.0.2.nuspec.xml"))
            {
                spec = _sut.ParseSpec(content);
            }

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

        // version in the spec 1.0 must converted to 1.0.0
        [Test]
        public void ParseSpecOwin()
        {
            NuGetSpec spec;
            using (var content = TempFile.OpenResource(GetType(), "NuGetApiTest.Owin.1.0.nuspec.xml"))
            {
                spec = _sut.ParseSpec(content);
            }

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
        public void Parse2010SpecCommonLogging()
        {
            NuGetSpec spec;
            using (var content = TempFile.OpenResource(GetType(), "NuGetApiTest.Common.Logging.2.0.0.nuspec.xml"))
            {
                spec = _sut.ParseSpec(content);
            }

            spec.ShouldNotBeNull();
            spec.Id.ShouldBe("Common.Logging");
            spec.Version.ShouldBe("2.0.0");
            spec.PackageHRef.ShouldBe("https://www.nuget.org/packages/Common.Logging/2.0.0");
            spec.Description.ShouldBe("Common.Logging library introduces a simple abstraction to allow you to select a specific logging implementation at runtime.");
            spec.Authors.ShouldBe("Aleksandar Seovic, Mark Pollack, Erich Eichinger");
            spec.Copyright.ShouldBeNull();

            spec.License.ShouldBeNull();
            spec.LicenseUrl.ShouldBeNull();
            spec.ProjectUrl.ShouldBe("http://netcommon.sourceforge.net/");
            spec.Repository.ShouldBeNull();
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
        [TestCase("license", "Copyright (c) Tunnel Vision Laboratories")]
        [TestCase("LICENSE.txt", null)]
        [TestCase("tools/install.ps1", "param($installPath, $toolsPath, $package, $project)")]
        [TestCase("tools\\install.ps1", "param($installPath, $toolsPath, $package, $project)")]
        public async Task LoadFileContentStyleCopAnalyzers(string fileName, string expected)
        {
            using (var content = TempFile.OpenResource(GetType(), "NuGetApiTest.StyleCop.Analyzers.1.1.118.nupkg"))
            {
                var file = await _sut.LoadFileContentAsync(await content.ToArrayAsync(CancellationToken.None), fileName, CancellationToken.None);

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
        }

        [Test]
        public async Task DownloadPackageStyleCopAnalyzersFromWeb()
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");

            _mockHttp
                .When(HttpMethod.Get, NuGetApi.Host + "/v3-flatcontainer/stylecop.analyzers/1.1.118/stylecop.analyzers.1.1.118.nupkg")
                .Respond(
                    MediaTypeNames.Application.Octet,
                    TempFile.OpenResource(GetType(), "NuGetApiTest.StyleCop.Analyzers.1.1.118.nupkg"));

            var file = await _sut.DownloadPackageAsync(package, false, CancellationToken.None);

            file.ShouldNotBeNull();
        }

        [Test]
        public async Task DownloadPackageStyleCopAnalyzersFromLocalCache()
        {
            var package = new NuGetPackageId("StyleCop.Analyzers", "1.1.118");

            var file = await _sut.DownloadPackageAsync(package, true, CancellationToken.None);

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
