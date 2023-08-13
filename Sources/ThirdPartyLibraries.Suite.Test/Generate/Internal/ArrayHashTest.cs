using System;
using System.Text;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

[TestFixture]
public class ArrayHashTest
{
    [Test]
    [TestCase(0, "")]
    [TestCase(1, "00")]
    [TestCase(2, "0001")]
    [TestCase(3, "000102")]
    [TestCase(4, "00010203")]
    [TestCase(5, "0001020304")]
    [TestCase(8, "0001020304050607")]
    public void Write(int bytesCount, string expected)
    {
        var sut = new ArrayHash(
            BitConverter.ToInt32(new byte[] { 0, 1, 2, 3 }),
            BitConverter.ToInt32(new byte[] { 4, 5, 6, 7 }));
        
        var actual = new StringBuilder();
        sut.ToString(actual, bytesCount);

        actual.ToString().ShouldBe(expected);
    }
}