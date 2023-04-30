using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.NuGet;

[TestFixture]
public class NuGetMetadataParserTest
{
    [Test]
    public void ParseVersion2()
    {
        using var stream = TempFile.OpenResource(GetType(), "NuGetMetadataParserTest.metadata2.json");

        var actual = NuGetMetadataParser.Parse(stream);

        actual.Version.ShouldBe(2);
        actual.Source.ShouldBe("https://nuget.pkg.github.com/organization/index.json");
    }

    [Test]
    public void ParseVersion1()
    {
        using var stream = TempFile.OpenResource(GetType(), "NuGetMetadataParserTest.metadata1.json");

        var actual = NuGetMetadataParser.Parse(stream);

        actual.Version.ShouldBe(1);
        actual.Source.ShouldBeNull();
    }
}