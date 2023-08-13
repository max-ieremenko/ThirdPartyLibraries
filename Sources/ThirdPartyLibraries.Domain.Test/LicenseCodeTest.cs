using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Domain;

[TestFixture]
public class LicenseCodeTest
{
    [Test]
    [TestCase("MIT", "MIT")]
    [TestCase(" mit ", "mit")]
    public void FromSimpleText(string text, string expected)
    {
        var sut = LicenseCode.FromText(text);

        sut.Text.ShouldBe(text);
        sut.Codes.ShouldBe(new[] { expected });
    }

    [Test]
    [TestCase("(MIT)", "MIT")]
    [TestCase("( MIT )", "MIT")]
    [TestCase("(MIT OR MIT)", "MIT")]
    [TestCase("MIT OR Apache-2.0", "Apache-2.0", "MIT")]
    [TestCase("EPL-1.0+", "EPL-1.0")]
    [TestCase("GPL-2.0-only", "GPL-2.0")]
    [TestCase("GPL-2.0-or-later", "GPL-2.0")]
    [TestCase("GPL-3.0-only WITH Classpath-exception-2.0", "Classpath-exception-2.0", "GPL-3.0")]
    [TestCase("Apache-2.0 AND (MIT OR GPL-2.0-only)", "Apache-2.0", "GPL-2.0", "MIT")]
    public void FromExpression(string text, params string[] expectedCodes)
    {
        var sut = LicenseCode.FromText(text);

        sut.Text.ShouldBe(text);
        sut.Codes.ShouldBe(expectedCodes);
    }

    [Test]
    [TestCase("MIT AND MIT+", "[MIT] AND [MIT]+")]
    [TestCase("MIT AND MIT2+", "[MIT] AND [MIT 2]+")]
    [TestCase("GPL-2.0-or-later", "[GPL-2.0]-or-later")]
    [TestCase("Apache-2.0 AND (MIT OR GPL-2.0-only)", "[Apache] AND ([MIT] OR [GPL-2.0]-only)")]
    public void ReplaceCodes(string text, string expected)
    {
        var replacementByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Apache-2.0", "[Apache]" },
            { "MIT", "[MIT]" },
            { "MIT2", "[MIT 2]" },
            { "GPL-2.0", "[GPL-2.0]" }
        };

        var sut = LicenseCode.FromText(text);

        sut.ReplaceCodes(i => replacementByCode[i]).ShouldBe(expected);
    }

    [Test]
    [TestCase(" ", null)]
    [TestCase("MIT AND mit2", "MIT2 OR MIT")]
    [TestCase("MIT+", "MIt")]
    public void Equals(string? text1, string? text2)
    {
        var sut1 = LicenseCode.FromText(text1);
        var sut2 = LicenseCode.FromText(text2);

        sut1.ShouldBe(sut2);
        sut2.ShouldBe(sut1);
        sut1.GetHashCode().ShouldBe(sut2.GetHashCode());
    }

    [Test]
    [TestCase("MIT", null)]
    [TestCase("MIT AND MIT2", "MIT")]
    [TestCase("MIT AND Apache-2.0", "MIT2 AND Apache-2.0")]
    public void NotEquals(string? text1, string? text2)
    {
        var sut1 = LicenseCode.FromText(text1);
        var sut2 = LicenseCode.FromText(text2);

        sut1.ShouldNotBe(sut2);
    }
}