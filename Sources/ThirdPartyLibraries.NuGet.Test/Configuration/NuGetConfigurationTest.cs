using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.NuGet.Internal;

namespace ThirdPartyLibraries.NuGet.Configuration;

[TestFixture]
public class NuGetConfigurationTest
{
    private IConfigurationRoot _configuration = null!;
    private NuGetConfiguration _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _sut = new NuGetConfiguration();
        _configuration = LoadConfiguration();
    }

    [Test]
    public void Bind()
    {
        _configuration.GetSection(NuGetLibraryId.PackageSource).Bind(_sut);

        _sut.AllowToUseLocalCache.ShouldBeTrue();
        _sut.DownloadPackageIntoRepository.ShouldBeTrue();

        _sut.IgnorePackages.ByName.ShouldBe(new[] { "Abc.*" });
        _sut.IgnorePackages.ByProjectName.ShouldBeEmpty();

        _sut.InternalPackages.ByName.ShouldBe(new[] { "StyleCop\\.Analyzers" });
        _sut.InternalPackages.ByProjectName.ShouldBe(new[] { "\\.Test$" });
    }

    private IConfigurationRoot LoadConfiguration()
    {
        using var file = TempFile.FromResource(GetType(), "NuGetConfigurationTest.appsettings.json");

        return new ConfigurationBuilder()
            .AddJsonFile(file.Location, false, false)
            .Build();
    }
}