using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Shared;

[TestFixture]
public class UriSimpleComparerTest
{
    [Test]
    [TestCaseSource(nameof(GetEqualsCases))]
    public void Equals(string x, string y, bool expected)
    {
        var uriX = new Uri(x);
        var uriY = new Uri(y);

        UriSimpleComparer.Equals(uriX, uriY).ShouldBe(expected);
        UriSimpleComparer.Equals(uriY, uriX).ShouldBe(expected);

        if (expected)
        {
            UriSimpleComparer.Instance.GetHashCode(uriX).ShouldBe(UriSimpleComparer.Instance.GetHashCode(uriY));
        }
    }

    [Test]
    [TestCaseSource(nameof(GetDefaultComparerBehaviourCases))]
    public void DefaultComparerBehaviour(string x, string y, bool expected)
    {
        new Uri(x).Equals(new Uri(y)).ShouldBe(expected);
    }

    [Test]
    [TestCase("/directory/rest/", "directory", "rest")]
    [TestCase("/directory/rest1/rest2", "directory", "rest1/rest2")]
    [TestCase("/directory/", "directory", "")]
    [TestCase("/", "", "")]
    public void GetDirectoryName(string path, string expected, string expectedRest)
    {
        UriSimpleComparer.GetDirectoryName(path, out var actual, out var actualRest).ShouldBe(expected.Length > 0);

        actual.ToString().ShouldBe(expected);
        actualRest.ToString().ShouldBe(expectedRest);
    }

    private static IEnumerable<TestCaseData> GetEqualsCases()
    {
        yield return new TestCaseData("http://host", "https://host", true)
        {
            TestName = "http == https"
        };

        yield return new TestCaseData("ftp://host", "ftp://host", true)
        {
            TestName = "ftp == ftp"
        };

        yield return new TestCaseData("ftp://host", "http://host", false)
        {
            TestName = "http != ftp"
        };

        yield return new TestCaseData("https://host:80", "https://host:443", true)
        {
            TestName = "port does not matter"
        };

        yield return new TestCaseData("http://host/", "http://host", true)
        {
            TestName = "ending / does not matter"
        };

        yield return new TestCaseData("http://host/path", "http://host/PATH", true)
        {
            TestName = "path is case-insensitive"
        };

        yield return new TestCaseData("http://host/?foo", "http://host/?FOO", true)
        {
            TestName = "query is case-insensitive"
        };

        yield return new TestCaseData("http://host1", "http://host2", false)
        {
            TestName = "host1 != host2"
        };

        yield return new TestCaseData("http://host/path1", "http://host/path2", false)
        {
            TestName = "path1 != path2"
        };

        yield return new TestCaseData("http://host/path?", "http://host/path", true)
        {
            TestName = "empty ? does not matter"
        };

        yield return new TestCaseData("http://host/path/?", "http://host/path", true)
        {
            TestName = "empty /? does not matter"
        };

        yield return new TestCaseData("http://host/path?foo", "http://host/path?bar", false)
        {
            TestName = "query1 != query2"
        };
    }

    private static IEnumerable<TestCaseData> GetDefaultComparerBehaviourCases()
    {
        yield return new TestCaseData("http://host.name", "http://HOST.name", true)
        {
            TestName = "host name is case-insensitive"
        };

        yield return new TestCaseData("https://host", "http://host", false)
        {
            TestName = "https != https"
        };

        yield return new TestCaseData("https://host:80", "https://host:443", false)
        {
            TestName = "port1 != port2"
        };

        yield return new TestCaseData("http://host/path", "http://host/PATH", false)
        {
            TestName = "path is case-sensitive"
        };

        yield return new TestCaseData("http://host/?foo", "http://host/?FOO", false)
        {
            TestName = "query is case-sensitive"
        };

        yield return new TestCaseData("http://host/path?", "http://host/path", false)
        {
            TestName = "empty ? matters"
        };

        yield return new TestCaseData("http://host/path/?", "http://host/path", false)
        {
            TestName = "empty /? matters"
        };
    }
}