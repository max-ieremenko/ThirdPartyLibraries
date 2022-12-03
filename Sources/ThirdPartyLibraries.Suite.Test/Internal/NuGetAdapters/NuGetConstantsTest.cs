using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters;

[TestFixture]
public class NuGetConstantsTest
{
    [Test]
    [TestCase("https://aka.ms/deprecateLicenseUrl", true)]
    [TestCase("http://aka.ms/deprecateLicenseUrl", true)]
    [TestCase("https://aka2.ms/deprecateLicenseUrl", false)]
    public void IsDeprecateLicenseUrl(string url, bool expected)
    {
        NuGetConstants.IsDeprecateLicenseUrl(url).ShouldBe(expected);
    }
}