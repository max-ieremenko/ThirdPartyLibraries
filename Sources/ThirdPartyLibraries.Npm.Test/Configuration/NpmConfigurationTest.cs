using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Npm.Internal;

namespace ThirdPartyLibraries.Npm.Configuration;

[TestFixture]
public class NpmConfigurationTest
{
    private IConfigurationRoot _configuration = null!;
    private NpmConfiguration _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _sut = new NpmConfiguration();
        _configuration = LoadConfiguration();
    }

    [Test]
    public void Bind()
    {
        _configuration.GetSection(NpmLibraryId.PackageSource).Bind(_sut);

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