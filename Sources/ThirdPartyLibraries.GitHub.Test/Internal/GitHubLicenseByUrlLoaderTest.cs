using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.GitHub.Internal;

[TestFixture]
public class GitHubLicenseByUrlLoaderTest
{
    private Mock<IGitHubRepository> _repository = null!;
    private GitHubLicenseByUrlLoader _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _repository = new Mock<IGitHubRepository>(MockBehavior.Strict);
        _sut = new GitHubLicenseByUrlLoader(_repository.Object);
    }

    [Test]
    public async Task DownloadNewtonsoftAsync()
    {
        _repository
            .Setup(r => r.GetAsJsonAsync("https://api.github.com/repos/JamesNK/Newtonsoft.Json/license", default))
            .ReturnsAsync(TempFile.OpenResource(GetType(), "GitHubLicenseByUrlLoaderTest.License.Newtonsoft.json").JsonDeserialize<JObject>());

        var actual = await _sut.TryDownloadAsync(new Uri("https://github.com/JamesNK/Newtonsoft.Json"), default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Source.ShouldBe(LicenseSpecSource.UserDefined);
        actual.Code.ShouldBe("MIT");
        actual.FullName.ShouldBe("MIT License");
        actual.FileName.ShouldBe("LICENSE.md");
        actual.FileExtension.ShouldBe(".md");
        actual.FileContent.ShouldNotBeNull();
        actual.FileContent.AsText().ShouldContain("James Newton-King");
    }

    [Test]
    public async Task DownloadShouldlyAsync()
    {
        _repository
            .Setup(r => r.GetAsJsonAsync("https://api.github.com/repos/shouldly/shouldly/license", default))
            .ReturnsAsync(TempFile.OpenResource(GetType(), "GitHubLicenseByUrlLoaderTest.License.Shouldly.json").JsonDeserialize<JObject>());

        var actual = await _sut.TryDownloadAsync(new Uri("https://github.com/shouldly/shouldly"), default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Source.ShouldBe(LicenseSpecSource.UserDefined);
        actual.Code.ShouldBe("NOASSERTION");
        actual.FullName.ShouldBeNull();
        actual.FileName.ShouldBe("LICENSE.txt");
        actual.FileExtension.ShouldBe(".txt");
        actual.FileContent.ShouldNotBeNull();
        Console.WriteLine(actual.FileContent.AsText());
        actual.FileContent.AsText().ShouldContain("Redistribution and use in source and binary forms");
    }

    [Test]
    public async Task DownloadMitLicenseAsync()
    {
        _repository
            .Setup(r => r.GetAsJsonAsync("https://api.github.com/licenses/mit", default))
            .ReturnsAsync(TempFile.OpenResource(GetType(), "GitHubLicenseByUrlLoaderTest.License.MIT.json").JsonDeserialize<JObject>());

        var actual = await _sut.TryDownloadAsync(new Uri("https://api.github.com/licenses/mit"), default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Source.ShouldBe(LicenseSpecSource.Shared);
        actual.Code.ShouldBe("MIT");
        actual.FullName.ShouldBe("MIT License");
        actual.FileName.ShouldBeNull();
        actual.FileExtension.ShouldBe(".txt");
        actual.FileContent.ShouldNotBeNull();
        actual.FileContent.AsText().ShouldContain("MIT License");
    }
}