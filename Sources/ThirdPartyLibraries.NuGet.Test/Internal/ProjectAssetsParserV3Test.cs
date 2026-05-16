using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.NuGet.Internal;

[TestFixture]
public class ProjectAssetsParserV3Test
{
    [Test]
    public void GetTargetFrameworks()
    {
        var actual = CreateSut().GetTargetFrameworks();

        actual.ShouldBe(["net452", "netcoreapp2.2", "net472"], ignoreOrder: true);
    }

    [Test]
    public void GetNet452References()
    {
        var actual = CreateSut().GetReferences("net452").ToList();

        actual.Count.ShouldBe(1);

        actual[0].Package.Name.ShouldBe("StyleCop.Analyzers");
        actual[0].Package.Version.ShouldBe("1.1.118");
    }

    [Test]
    public void GetNetCore22References()
    {
        var actual = CreateSut().GetReferences("netcoreapp2.2").ToList();

        actual.Count.ShouldBe(11);
        actual[2].Package.Name.ShouldBe("System.Configuration.ConfigurationManager");
        actual[2].Package.Version.ShouldBe("4.5.0");

        var dependencies = actual[2].Dependencies;
        dependencies.Count.ShouldBe(2);

        dependencies[0].Name.ShouldBe("System.Security.Cryptography.ProtectedData");
        dependencies[0].Version.ShouldBe("4.5.0");

        dependencies[1].Name.ShouldBe("System.Security.Permissions");
        dependencies[1].Version.ShouldBe("4.5.0");
    }

    [Test]
    public void GetNet472References()
    {
        var actual = CreateSut().GetReferences("net472").ToList();

        actual.ShouldBeEmpty();
    }

    [Test]
    public void GetProjectName()
    {
        CreateSut().GetProjectName().ShouldBe("Company.Name.Project");
    }

    [Test]
    public void GetPackageSources()
    {
        CreateSut().GetPackageSources().ShouldBe(
        [
            new Uri(@"C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\", UriKind.Absolute),
            new Uri("https://api.nuget.org/v3/index.json", UriKind.Absolute)
        ]);
    }

    private static ProjectAssetsParser CreateSut()
    {
        var resourceName = "ProjectAssetsParserTest.projectV3.assets.json";
        using var stream = TempFile.OpenResource(typeof(ProjectAssetsParserTest), resourceName);
        return ProjectAssetsParser.FromStream(stream);
    }
}