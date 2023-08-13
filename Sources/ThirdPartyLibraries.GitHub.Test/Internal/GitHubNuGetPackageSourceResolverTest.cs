using System;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.GitHub.Internal;

[TestFixture]
public class GitHubNuGetPackageSourceResolverTest
{
    private const string Source = "https://nuget.pkg.github.com/org-name/index.json";

    private Mock<IPackageSpec> _spec = null!;
    private GitHubNuGetPackageSourceResolver _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _spec = new Mock<IPackageSpec>(MockBehavior.Strict);
        _sut = new GitHubNuGetPackageSourceResolver();
    }

    [Test]
    public void UrlBoundToRepo()
    {
        _spec
            .Setup(s => s.GetRepositoryUrl())
            .Returns("https://github.com/org-name/repo-name");

        _spec
            .Setup(s => s.GetName())
            .Returns("the.package.name");

        _sut.TryResolve(_spec.Object, new Uri(Source), out var actual).ShouldBeTrue();
        
        actual.ShouldNotBeNull();

        // ignore version
        //   github uses magic-id in the path insteadof version
        //   e.g. https://github.com/org-name/repo-name/pkgs/nuget/package-name/12345
        actual.Value.DownloadUrl.ShouldBe(new Uri("https://github.com/org-name/repo-name/pkgs/nuget/the.package.name"));
    }

    [Test]
    public void UrlBoundToPackage()
    {
        _spec
            .Setup(s => s.GetRepositoryUrl())
            .Returns((string?)null);

        _spec
            .Setup(s => s.GetName())
            .Returns("the.package.name");

        _sut.TryResolve(_spec.Object, new Uri(Source), out var actual).ShouldBeTrue();
        
        actual.ShouldNotBeNull();
        actual.Value.DownloadUrl.ShouldBe(new Uri("https://github.com/org-name/the/pkgs/nuget/the.package.name"));
    }
}