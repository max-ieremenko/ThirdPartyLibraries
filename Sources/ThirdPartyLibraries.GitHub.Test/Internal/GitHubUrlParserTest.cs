using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.GitHub.Internal;

[TestFixture]
public class GitHubUrlParserTest
{
    [Test]
    [TestCase("https://github.com/JamesNK/Newtonsoft.Json", "JamesNK", "Newtonsoft.Json")]
    [TestCase("https://api.github.com/repos/JamesNK/Newtonsoft.Json/license", "JamesNK", "Newtonsoft.Json")]
    [TestCase("https://github.com/shouldly/shouldly.git", "shouldly", "shouldly")]
    [TestCase("https://github.com/DefinitelyTyped/DefinitelyTyped#readme", "DefinitelyTyped", "DefinitelyTyped")]
    [TestCase("https://raw.github.com/moq/moq4/master/License.txt", "moq", "moq4")]
    [TestCase("https://raw.githubusercontent.com/moq/moq4/master/License.txt", "moq", "moq4")]
    [TestCase("git://github.com/dotnet/runtime", "dotnet", "runtime")]
    [TestCase("https://github.com/unitycontainer", null, null)]
    [TestCase("https://localhost/shouldly/shouldly", null, null)]
    [TestCase("https://api.github.com/licenses/mit", null, null)]
    [TestCase("https://api.github.com/JamesNK/Newtonsoft.Json/license", null, null)]
    public void TryParseRepository(string url, string? expectedOwner, string? expectedRepository)
    {
        var actual = GitHubUrlParser.TryParseRepository(new Uri(url), out var actualOwner, out var actualRepository);

        actual.ShouldBe(expectedRepository != null);
        actualOwner.ShouldBe(expectedOwner);
        actualRepository.ShouldBe(expectedRepository);
    }

    [Test]
    [TestCase("https://api.github.com/licenses/mit", "mit")]
    [TestCase("https://api.github.com/licenses/mit/1", null)]
    [TestCase("https://api.github.com/license/mit", null)]
    public void TryParseLicenseCode(string url, string? expected)
    {
        var actual = GitHubUrlParser.TryParseLicenseCode(new Uri(url), out var actualCode);

        actual.ShouldBe(expected != null);
        actualCode.ShouldBe(expected);
    }

    [Test]
    [TestCase("https://nuget.pkg.github.com/org-name/index.json", "org-name")]
    [TestCase("https://api.github.com/org-name/index.json", null)]
    public void TryParseNuGetOwner(string packageSource, string? expected)
    {
        var actual = GitHubUrlParser.TryParseNuGetOwner(new Uri(packageSource), out var actualOwner);

        actual.ShouldBe(expected != null);
        actualOwner.ShouldBe(expected);
    }
}