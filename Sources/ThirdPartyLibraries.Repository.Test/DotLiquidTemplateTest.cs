using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Repository.Template;

namespace ThirdPartyLibraries.Repository;

[TestFixture]
public class DotLiquidTemplateTest
{
    [Test]
    public void RenderThirdPartyNotices()
    {
        var license = new ThirdPartyNoticesLicenseContext
        {
            FullName = "license full name",
            FileNames = { "file 1", "file 2" },
            HRefs = { "lic ref 1", "lic ref 2" }
        };

        var package = new ThirdPartyNoticesPackageContext
        {
            Name = "package name",
            Author = "package author",
            Copyright = "package copyright",
            HRef = "package ref",
            ThirdPartyNotices = "package notices",
            License = license
        };

        license.Packages.Add(package);

        var context = new ThirdPartyNoticesContext
        {
            Title = "The Application",
            Licenses = { license },
            Packages = { package }
        };

        using var actual = Render(DotLiquidTemplate.GetThirdPartyNoticesTemplate(), context);

        var line = actual.ReadLine();
        line.ShouldBe(context.Title);

        line = actual.ReadLine();
        line.ShouldBe(new string('*', context.Title.Length));

        line = actual.ReadLine();
        line.ShouldBeEmpty();

        line = actual.ReadLine();
        line.ShouldBe("THIRD-PARTY SOFTWARE NOTICES AND INFORMATION");

        line = actual.ReadLine();
        line.ShouldBeEmpty();

        line = actual.ReadLine();
        line.ShouldBe("package name (package ref)");
    }

    [Test]
    public void RenderNuGetReadme()
    {
        var context = new LibraryReadMeContext
        {
            Name = "DotLiquid",
            Version = "2.3.197",
            Description = "DotLiquid is a templating system ported to the .NET framework from Ruby’s Liquid Markup.",
            HRef = "https://www.nuget.org/packages/DotLiquid/2.3.197",
            LicenseCode = "Apache-2.0 OR MS-PL",
            LicenseMarkdownExpression = "[Apache-2.0](../../../../licenses/apache-2.0) OR [MS-PL](../../../../licenses/ms-pl)",
            UsedBy = "ThirdPartyLibraries",
            TargetFrameworks = "netstandard2.1",
            Remarks = "some remarks",
            ThirdPartyNotices = "some notices",
            Dependencies =
            {
                new()
                {
                    Name = "dependency",
                    Version = "1.0",
                    LocalHRef = "../"
                }
            }
        };

        using var actual = Render(DotLiquidTemplate.GetLibraryReadMeTemplate("nuget.org"), context);
    }

    [Test]
    public void GetRootReadMeTemplate()
    {
        DotLiquidTemplate.GetRootReadMeTemplate().ShouldNotBeEmpty();
    }

    [Test]
    [TestCase("nuget.org")]
    [TestCase("npmjs.com")]
    public void GetLibraryReadMeTemplate(string librarySourceCode)
    {
        DotLiquidTemplate.GetLibraryReadMeTemplate(librarySourceCode).ShouldNotBeEmpty();
    }

    [Test]
    public void GetThirdPartyNoticesTemplate()
    {
        DotLiquidTemplate.GetThirdPartyNoticesTemplate().ShouldNotBeEmpty();
    }

    [Test]
    public void GetAppSettingsTemplate()
    {
        DotLiquidTemplate.GetAppSettingsTemplate().ShouldNotBeEmpty();
    }

    private static StreamReader Render(byte[] templateSource, object context)
    {
        var template = new StreamReader(new MemoryStream(templateSource)).ReadToEnd();

        var stream = new MemoryStream();
        DotLiquidTemplate.RenderTo(stream, template, context);
        
        stream.Seek(0, SeekOrigin.Begin);
        Output(stream);

        stream.Seek(0, SeekOrigin.Begin);
        return new StreamReader(stream);
    }

    [Conditional("DEBUG")]
    private static void Output(Stream stream)
    {
        Console.WriteLine("-----------");
        Console.WriteLine(new StreamReader(stream, leaveOpen: true).ReadToEnd());
        Console.WriteLine("-----------");
    }
}