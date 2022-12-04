using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.NuGet;

[TestFixture]
public class SemanticVersionTest
{
    [Test]
    [TestCase("1.0.0", "1.0.0", null)]
    [TestCase("1.0.0-alpha.1", "1.0.0-alpha.1", null)]
    [TestCase("1.0.0+githash", "1.0.0", "githash")]
    [TestCase("1.0.0-alpha.1+githash", "1.0.0-alpha.1", "githash")]
    public void Parse(string value, string version, string build)
    {
        var actual = new SemanticVersion(value);

        actual.Version.ShouldBe(version);
        actual.Build.ShouldBe(build);
    }
}