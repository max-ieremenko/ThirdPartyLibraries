using System.IO;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

[TestFixture]
public class ArrayHashBuilderTest
{
    [Test]
    public void EmptyStream()
    {
        var actual = ArrayHashBuilder.FromStream(new MemoryStream());

        actual.ShouldBeNull();
    }

    [Test]
    public void Deterministic()
    {
        var actual = ArrayHashBuilder.FromStream("Deterministic".AsStream());

        actual.ShouldNotBeNull();
        actual.ShouldBe(new ArrayHash(-331806516, 650765698, 205092669, -1093607938, 590118626));
    }
}