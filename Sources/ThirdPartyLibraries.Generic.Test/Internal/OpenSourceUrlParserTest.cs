using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Generic.Internal;

[TestFixture]
public class OpenSourceUrlParserTest
{
    [Test]
    [TestCaseSource(nameof(GetTryParseLicenseCodeCases))]
    public void TryParseLicenseCode(string url, string host, string directory, string? expected)
    {
        OpenSourceUrlParser.TryParseLicenseCode(new Uri(url), host, directory, out var actual).ShouldBe(expected != null);

        actual.Equals(expected.AsSpan(), StringComparison.Ordinal).ShouldBeTrue();
    }

    private static IEnumerable<TestCaseData> GetTryParseLicenseCodeCases()
    {
        yield return new TestCaseData("https://api.opensource.org/license/MIT/", "api.opensource.org", "license", "MIT")
        {
            TestName = "api.opensource.org/license/MIT"
        };

        yield return new TestCaseData("https://api.opensource.org/licenses/MIT", "api.opensource.org", "licenses", "MIT")
        {
            TestName = "api.opensource.org/licenses/MIT"
        };

        yield return new TestCaseData("https://opensource.org/license/MIT", "opensource.org", "license", "MIT")
        {
            TestName = "opensource.org/license/MIT"
        };

        yield return new TestCaseData("https://opensource.org/licenses/MIT", "opensource.org", "licenses", "MIT")
        {
            TestName = "opensource.org/licenses/MIT"
        };

        yield return new TestCaseData("https://spdx.org/licenses/MIT.html", "spdx.org", "licenses", "MIT.html")
        {
            TestName = "spdx.org/licenses/MIT.html"
        };

        yield return new TestCaseData("https://localhost/licenses/MIT", "spdx.org", "licenses", null)
        {
            TestName = "localhost/licenses/MIT"
        };

        yield return new TestCaseData("https://spdx.org/unknown-path/MIT", "spdx.org", "licenses", null)
        {
            TestName = "unknown-path/licenses/MIT"
        };

        yield return new TestCaseData("https://spdx.org/licenses/MIT/invalid", "spdx.org", "licenses", null)
        {
            TestName = "spdx.org/licenses/MIT/invalid"
        };
    }
}