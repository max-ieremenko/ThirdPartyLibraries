using System.Net.Mime;
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
    public async Task DownloadPackageNunitFromLocalCache()
    {
        var fileVersion = FileVersionInfo.GetVersionInfo(typeof(TestAttribute).Assembly.Location);
        var version = $"{fileVersion.FileMajorPart}.{fileVersion.FileMinorPart}.{fileVersion.FileBuildPart}";

        var path = Path
            .Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @".nuget/packages",
                "nunit",
                version)
            .ToLowerInvariant();
        Console.WriteLine(path);
        Assert.That(path, Does.Exist.IgnoreFiles);

        var file = await _sut.TryGetPackageFromCacheAsync("NUnit", version, new List<Uri>(), default).ConfigureAwait(false);

        file.ShouldNotBeNull();
    }
}