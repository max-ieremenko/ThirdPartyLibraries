using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet;

[TestFixture]
public class ProjectAssetsParserTest
{
    [Test]
    public void GetTargetFrameworks()
    {
        var actual = CreateSut("project").GetTargetFrameworks();

        actual.ShouldBe(new[] { "net452", "netcoreapp2.2", "net472" }, ignoreOrder: true);
    }

    [Test]
    public void GetNet452References()
    {
        var actual = CreateSut("project").GetReferences("net452").ToList();

        actual.Count.ShouldBe(1);

        actual[0].Package.Name.ShouldBe("StyleCop.Analyzers");
        actual[0].Package.Version.ShouldBe("1.1.118");
    }

    [Test]
    public void GetNetCore22References()
    {
        var actual = CreateSut("project").GetReferences("netcoreapp2.2").ToList();

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
        var actual = CreateSut("project").GetReferences("net472").ToList();

        actual.ShouldBeEmpty();
    }

    [Test]
    public void GetProjectName()
    {
        CreateSut("project").GetProjectName().ShouldBe("Company.Name.Project");
    }

    [Test]
    public void InvalidReference()
    {
        var sut = CreateSut("invalid-project");

        var ex = Assert.Throws<InvalidOperationException>(() => sut.GetReferences("netcoreapp3.1"));
        Console.WriteLine(ex);

        ex.Message.ShouldContain("Company.Name.Project");
        ex.Message.ShouldContain("StyleCop.Analyzers");
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

    private static ProjectAssetsParser CreateSut(string fileName)
    {
        using (var stream = TempFile.OpenResource(typeof(ProjectAssetsParserTest), "ProjectAssetsParserTest.{0}.assets.json".FormatWith(fileName)))
        {
            return ProjectAssetsParser.FromStream(stream);
        }
    }
}