using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters;

[TestFixture]
public class NpmConfigurationTest
{
    private IConfigurationRoot _configuration;
    private NpmConfiguration _sut;

    [SetUp]
    public void BeforeEachTest()
    {
        _sut = new NpmConfiguration();
        _configuration = LoadConfiguration();
    }

    [Test]
    public void Bind()
    {
        _configuration.GetSection(PackageSources.Npm).Bind(_sut);

        _sut.DownloadPackageIntoRepository.ShouldBeTrue();

        _sut.IgnorePackages.ByName.ShouldBe(new[] { "Abc.*" });
        _sut.IgnorePackages.ByFolderName.ShouldBe(new[] { "\\.Demo$" });
    }

    private IConfigurationRoot LoadConfiguration()
    {
        using var file = TempFile.FromResource(GetType(), "NpmConfigurationTest.appsettings.json");

        return new ConfigurationBuilder()
            .AddJsonFile(file.Location, false, false)
            .Build();
    }
}