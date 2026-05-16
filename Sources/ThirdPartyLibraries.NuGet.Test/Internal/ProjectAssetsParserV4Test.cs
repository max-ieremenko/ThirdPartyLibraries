using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.NuGet.Internal;

[TestFixture]
public class ProjectAssetsParserV4Test
{
    [Test]
    public void GetTargetFrameworks()
    {
        var actual = CreateSut().GetTargetFrameworks();

        actual.ShouldBe(["netstandard2.1"]);
    }

    [Test]
    public void GetNetStandard21References()
    {
        var actual = CreateSut().GetReferences("netstandard2.1").ToList();

        actual.Count.ShouldBe(12);

        actual[1].Package.Name.ShouldBe("Microsoft.Extensions.Configuration.Binder");
        actual[1].Package.Version.ShouldBe("10.0.0");
        actual[1].Dependencies.Select(i => i.Name).ShouldBe(["Microsoft.Extensions.Configuration", "Microsoft.Extensions.Configuration.Abstractions"]);
    }

    [Test]
    public void GetProjectName()
    {
        CreateSut().GetProjectName().ShouldBe("Company.Name.Project");
    }

    [Test]
    public void GetPackageSources()
    {
        CreateSut().GetPackageSources().ShouldBe([new Uri("https://api.nuget.org/v3/index.json", UriKind.Absolute)]);
    }

    private static ProjectAssetsParser CreateSut()
    {
        var resourceName = "ProjectAssetsParserTest.projectV4.assets.json";
        using var stream = TempFile.OpenResource(typeof(ProjectAssetsParserTest), resourceName);
        return ProjectAssetsParser.FromStream(stream);
    }
}