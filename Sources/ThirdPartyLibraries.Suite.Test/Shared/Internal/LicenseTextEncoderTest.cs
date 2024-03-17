using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Suite.Shared.Internal;

[TestFixture]
internal class LicenseTextEncoderTest
{
    [Test]
    [TestCaseSource(nameof(GetConvertCases))]
    public void Convert(string? content, string text, string expected)
    {
        string actual;
        using (var sut = new LicenseTextEncoder())
        {
            if (content != null)
            {
                sut.Convert(content.ToCharArray(), content.Length);
            }

            sut.Convert(text.ToCharArray(), text.Length);
            actual = GetText(sut);
        }

        actual.ShouldBe(expected);
    }

    [Test]
    [TestCase("\r\n", true)]
    [TestCase("\t\n", true)]
    [TestCase("\tfoo\n", false)]
    public void IsEmpty(string text, bool expected)
    {
        using (var sut = new LicenseTextEncoder())
        {
            sut.Convert(text.ToCharArray(), text.Length);
            sut.IsEmpty.ShouldBe(expected);
        }
    }

    [Test]
    [Explicit]
    public void IntegrationTest()
    {
        string text;
        using (var reader = File.OpenText(@"ThirdPartyLibraries/licenses/apache-2.0/license.txt"))
        using (var sut = new LicenseTextEncoder())
        {
            var buffer = new char[1024];
            int length;
            while ((length = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                sut.Convert(buffer, length);
            }

            text = GetText(sut);
        }

        Console.WriteLine(text);
    }

    private static string GetText(LicenseTextEncoder sut)
    {
        var stream = new MemoryStream(sut.GetBuffer(), 0, sut.BufferLength);
        return new StreamReader(stream).ReadToEnd();
    }

    private static IEnumerable<TestCaseData> GetConvertCases()
    {
        yield return new TestCaseData(null, "\r\n", string.Empty)
        {
            TestName = "ignore line breaks"
        };

        yield return new TestCaseData("foo", "bar", "foobar")
        {
            TestName = "word"
        };

        yield return new TestCaseData("\t \t foo ", " bar\t", "foo bar")
        {
            TestName = "trim tabs and spaces"
        };

        yield return new TestCaseData(null, "foo\tbar", "foo bar")
        {
            TestName = "replace tabs with spaces"
        };

        yield return new TestCaseData(null, "foo \t  bar", "foo bar")
        {
            TestName = "keep only single space"
        };

        foreach (var separator in new[] { ',', '.', ';', '?', '!', '-', '(', ')' })
        {
            yield return new TestCaseData(
                null,
                "foo  " + separator,
                "foo" + separator)
            {
                TestName = "remove space before " + separator
            };
        }

        foreach (var separator in new[] { '-', '(', ')', '.' })
        {
            yield return new TestCaseData(
                null,
                separator + " \t content",
                separator + "content")
            {
                TestName = "remove space after " + separator
            };
        }
    }
}