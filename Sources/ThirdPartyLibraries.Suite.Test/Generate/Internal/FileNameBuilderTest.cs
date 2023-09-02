using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

[TestFixture]
public class FileNameBuilderTest
{
    [Test]
    [TestCaseSource(nameof(GetExpandCases))]
    public void Expand(object builder, string[] expected)
    {
        var sut = (FileNameBuilder)builder;

        sut.ToString().ShouldBe(expected[0]);
        for (var i = 1; i < expected.Length; i++)
        {
            sut.Expand();
            sut.ToString().ShouldBe(expected[i]);
        }
    }

    private static IEnumerable<TestCaseData> GetExpandCases()
    {
        var hash = new ArrayHash(
            BitConverter.ToInt32(new byte[] { 1, 2, 3, 4 }),
            BitConverter.ToInt32(new byte[] { 5, 6, 7, 8 }));

        yield return new TestCaseData(
            new FileNameBuilder("package", "1.0", ".TXT", hash),
            new[]
            {
                "package.txt",
                "package-1.0.txt",
                "package-1.0_010203.txt",
                "package-1.0_0102030405.txt"
            })
        {
            TestName = "full name"
        };

        yield return new TestCaseData(
            new FileNameBuilder("package", null, null, hash),
            new[]
            {
                "package",
                "package_010203",
                "package_0102030405"
            })
        {
            TestName = "name only"
        };

        yield return new TestCaseData(
            new FileNameBuilder("package", null, ".txt", hash),
            new[]
            {
                "package.txt",
                "package_010203.txt",
                "package_0102030405.txt"
            })
        {
            TestName = "name + ext"
        };

        yield return new TestCaseData(
            new FileNameBuilder("package", "1.0", null, hash),
            new[]
            {
                "package",
                "package-1.0",
                "package-1.0_010203",
                "package-1.0_0102030405"
            })
        {
            TestName = "name + suffix"
        };
    }
}