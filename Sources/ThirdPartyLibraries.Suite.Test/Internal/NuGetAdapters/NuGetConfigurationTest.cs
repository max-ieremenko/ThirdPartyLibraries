using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters;

[TestFixture]
public class NuGetConfigurationTest
{
    private IConfigurationRoot _configuration;
    private NuGetConfiguration _sut;

    [SetUp]
    public void BeforeEachTest()
    {
        _sut = new NuGetConfiguration();
        _configuration = LoadConfiguration();
    }

    [Test]
    public void Bind()
    {
        _configuration.GetSection(PackageSources.NuGet).Bind(_sut);

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