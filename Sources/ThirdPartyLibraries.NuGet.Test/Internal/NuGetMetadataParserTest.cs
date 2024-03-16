using System;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.NuGet.Internal;

[TestFixture]
public class NuGetMetadataParserTest
{
    [Test]
    public void ParseVersion2()
    {
        using var stream = TempFile.OpenResource(GetType(), "NuGetMetadataParserTest.metadata2.json");

        NuGetMetadataParser.TryGetSource(stream, out var actual).ShouldBeTrue();

        actual.ShouldBe(new Uri("https://nuget.pkg.github.com/organization/index.json", UriKind.Absolute));
    }

    [Test]
    public void ParseVersion1()
    {
        using var stream = TempFile.OpenResource(GetType(), "NuGetMetadataParserTest.metadata1.json");

        NuGetMetadataParser.TryGetSource(stream, out var actual).ShouldBeFalse();

        actual.ShouldBeNull();
    }
}