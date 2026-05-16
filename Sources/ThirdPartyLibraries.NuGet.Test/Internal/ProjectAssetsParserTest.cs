using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.NuGet.Internal;

[TestFixture]
public class ProjectAssetsParserTest
{
    [Test]
    public void InvalidReference()
    {
        var sut = CreateSut();

        var ex = Assert.Throws<InvalidOperationException>(() => sut.GetReferences("netcoreapp3.1"));
        
        ex.ShouldNotBeNull();
        Console.WriteLine(ex);

        ex.Message.ShouldContain("Company.Name.Project");
        ex.Message.ShouldContain("StyleCop.Analyzers");
    }

    [Test]
    public void GetPackageSourcesNotFound()
    {
        CreateSut().GetPackageSources().ShouldBeEmpty();
    }

    [Test]
    [TestCase("netcoreapp2.2", ".NETCoreApp,Version=v2.2")]
    [TestCase("netstandard1.3", ".NETStandard,Version=v1.3")]
    [TestCase("netstandard2.0", ".NETStandard,Version=v2.0")]
    [TestCase("net20", ".NETFramework,Version=v2.0")]
    [TestCase("net35", ".NETFramework,Version=v3.5")]
    [TestCase("net35-client", ".NETFramework,Version=v3.5,Profile=Client")]
    [TestCase("net451", ".NETFramework,Version=v4.5.1")]
    [TestCase("net452", ".NETFramework,Version=v4.5.2")]
    [TestCase("net472", ".NETFramework,Version=v4.7.2")]
    [TestCase("net48", ".NETFramework,Version=v4.8")]
    [TestCase("net6.0", "net6.0")]
    [TestCase("net6.0-windows7.0", "net6.0-windows7.0")]
    public void MapTargetFrameworkProjFormatToNuGetFormat(string projFormat, string expected)
    {
        ProjectAssetsParser.MapTargetFrameworkProjFormatToNuGetFormat(projFormat).ShouldBe(expected);
    }

    private static ProjectAssetsParser CreateSut()
    {
        var resourceName = "ProjectAssetsParserTest.invalid-project.assets.json";
        using var stream = TempFile.OpenResource(typeof(ProjectAssetsParserTest), resourceName);
        return ProjectAssetsParser.FromStream(stream);
    }
}