using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.NuGet.Internal;

[TestFixture]
public class NuGetSpecLicenseResolverTest
{
    [Test]
    [TestCase("https://aka.ms/deprecateLicenseUrl", true)]
    [TestCase("http://aka.ms/deprecateLicenseUrl", true)]
    [TestCase("https://aka2.ms/deprecateLicenseUrl", false)]
    public void IsDeprecateLicenseUrl(string url, bool expected)
    {
        NuGetSpecLicenseResolver.IsDeprecateLicenseUrl(url).ShouldBe(expected);
    }
}