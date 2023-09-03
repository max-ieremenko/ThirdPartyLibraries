using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Generic.Configuration;

[TestFixture]
public class StaticLicenseConfigurationTest
{
    private StaticLicenseConfiguration _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _sut = new StaticLicenseConfiguration();

        using (var file = TempFile.FromResource(GetType(), "StaticLicenseConfigurationTest.appsettings.json"))
        {
            var root = new ConfigurationBuilder()
                .AddJsonFile(file.Location, false, false)
                .Build();

            root.GetSection(StaticLicenseConfiguration.SectionName).Bind(_sut);
        }
    }

    [Test]
    public void ByCode()
    {
        _sut.ByCode.Count.ShouldBe(2);

        _sut.ByCode[0].Code.ShouldBe("ms-net-library");
        _sut.ByCode[0].FullName.ShouldBe("MICROSOFT .NET LIBRARY");
        _sut.ByCode[0].DownloadUrl.ShouldBe("https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm");
    }

    [Test]
    public void ByUrl()
    {
        _sut.ByUrl.Count.ShouldBe(4);

        _sut.ByUrl[0].Code.ShouldBe("MIT");
        _sut.ByUrl[0].Urls!.Length.ShouldBe(1);
        _sut.ByUrl[0].Urls![0].ShouldBe("https://github.com/dotnet/corefx/blob/master/LICENSE.TXT");
    }
}