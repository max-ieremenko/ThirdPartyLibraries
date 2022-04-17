using System;
using System.Diagnostics;
using System.IO;
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