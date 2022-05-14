using System.IO;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Suite.Internal.NameCombiners;

[TestFixture]
public class StreamHashTest
{
    [Test]
    public void EmptyStream()
    {
        var actual = StreamHash.FromStream(new MemoryStream());

        actual.ShouldBe(StreamHash.Empty);
    }

    [Test]
    public void Deterministic()
    {
        var actual = StreamHash.FromStream("Deterministic".AsStream());

        actual.Value.ShouldBe(new byte[] { 246, 121, 134, 112, 236, 203, 68, 141, 189, 122, 230, 23, 7, 174, 215, 112, 3, 48, 228, 181 });
    }
}