using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using Shouldly;

namespace ThirdPartyLibraries.NuGet.Internal;

[TestFixture]
public class NuGetRepositoryTest
{
    private NuGetRepository _sut = null!;
    private MockHttpMessageHandler _mockHttp = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _mockHttp = new MockHttpMessageHandler();

        _sut = new NuGetRepository(_mockHttp.ToHttpClient);
    }

    [Test]
    public async Task DownloadPackageStyleCopAnalyzersFromWeb()
    {
        _mockHttp
            .When(HttpMethod.Get, NuGetRepository.Host + "/v3-flatcontainer/stylecop.analyzers/1.1.118/stylecop.analyzers.1.1.118.nupkg")
            .Respond(
                MediaTypeNames.Application.Octet,
                TempFile.OpenResource(GetType(), "NuGetPackageTest.StyleCop.Analyzers.1.1.118.nupkg"));

        var file = await _sut.TryDownloadPackageAsync("StyleCop.Analyzers", "1.1.118", default).ConfigureAwait(false);

        file.ShouldNotBeNull();
    }

    [Test]
    public async Task DownloadPackageNewtonsoftJsonFromLocalCache()
    {
        var fileVersion = FileVersionInfo.GetVersionInfo(typeof(JsonSerializer).Assembly.Location);
        var version = $"{fileVersion.FileMajorPart}.{fileVersion.FileMinorPart}.{fileVersion.FileBuildPart}";

        var path = Path
            .Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @".nuget/packages",
                "Newtonsoft.Json",
                version)
            .ToLowerInvariant();
        Console.WriteLine(path);
        Assert.That(path, Does.Exist.IgnoreFiles);

        var file = await _sut.TryGetPackageFromCacheAsync("Newtonsoft.Json", version, new List<Uri>(), default).ConfigureAwait(false);

        file.ShouldNotBeNull();
    }
}