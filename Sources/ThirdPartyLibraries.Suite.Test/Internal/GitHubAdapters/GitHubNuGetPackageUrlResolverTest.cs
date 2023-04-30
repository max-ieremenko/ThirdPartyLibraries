using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.GitHub;

namespace ThirdPartyLibraries.Suite.Internal.GitHubAdapters;

[TestFixture]
public class GitHubNuGetPackageUrlResolverTest
{
    private const string Source = "https://nuget.pkg.github.com/org-name/index.json";

    private Mock<IGitHubApi> _gitHubApi;
    private GitHubNuGetPackageUrlResolver _sut;

    [SetUp]
    public void BeforeEachTest()
    {
        _gitHubApi = new Mock<IGitHubApi>(MockBehavior.Strict);
        _sut = new GitHubNuGetPackageUrlResolver(_gitHubApi.Object);
    }

    [Test]
    public void GetUserUrlBoundToRepo()
    {
        var repositoryUrl = "https://github.com/org-name/repo-name";
        var owner = "org-name";
        var repoName = "repo-name";

        _gitHubApi
            .Setup(g => g.TryExtractRepositoryName(repositoryUrl, out owner, out repoName))
            .Returns(true);

        var actual = _sut.GetUserUrl("package-name", "package-version", Source, repositoryUrl);

        // ignore version
        //   github uses magic-id in the path insteadof version
        //   e.g. https://github.com/org-name/repo-name/pkgs/nuget/package-name/12345
        actual.HRef.ShouldBe("https://github.com/org-name/repo-name/pkgs/nuget/package-name");
    }

    [Test]
    public void GetUserUrlBoundToPackage()
    {
        var actual = _sut.GetUserUrl("package.name", "package-version", Source, null);

        actual.HRef.ShouldBe("https://github.com/org-name/package/pkgs/nuget/package.name");
    }
}