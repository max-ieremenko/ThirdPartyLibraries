using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.NuGet.Internal;

[TestFixture]
public class NuGetPackageSpecTest
{
    [Test]
    [TestCaseSource(nameof(GetNameCases))]
    public void GetName(IPackageSpec sut, string expected) => sut.GetName().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetVersionCases))]
    public void GetVersion(IPackageSpec sut, string expected) => sut.GetVersion().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetDescriptionCases))]
    public void GetDescription(IPackageSpec sut, string? expected) => sut.GetDescription().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetLicenseTypeCases))]
    public void GetLicenseType(IPackageSpec sut, string? expected) => sut.ShouldBeOfType<NuGetPackageSpec>().GetLicenseType().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetLicenseValueCases))]
    public void GetLicenseValue(IPackageSpec sut, string? expected) => sut.ShouldBeOfType<NuGetPackageSpec>().GetLicenseValue().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetLicenseUrlCases))]
    public void GetLicenseUrl(IPackageSpec sut, string? expected) => sut.ShouldBeOfType<NuGetPackageSpec>().GetLicenseUrl().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetRepositoryUrlCases))]
    public void GetRepositoryUrl(IPackageSpec sut, string? expected) => sut.ShouldBeOfType<NuGetPackageSpec>().GetRepositoryUrl().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetProjectUrlCases))]
    public void GetProjectUrl(IPackageSpec sut, string? expected) => sut.ShouldBeOfType<NuGetPackageSpec>().GetProjectUrl().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetCopyrightCases))]
    public void GetCopyright(IPackageSpec sut, string? expected) => sut.GetCopyright().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetAuthorCases))]
    public void GetAuthor(IPackageSpec sut, string? expected) => sut.GetAuthor().ShouldBe(expected);

    private static IEnumerable<TestCaseData> GetNameCases()
    {
        const string testName = "Name";
        yield return CreateCommonLogging(testName, "Common.Logging");
        yield return CreateNewtonsoftJson(testName, "Newtonsoft.Json");
        yield return CreateOwin(testName, "Owin");
        yield return CreateStyleCopAnalyzers(testName, "StyleCop.Analyzers");
    }

    private static IEnumerable<TestCaseData> GetVersionCases()
    {
        const string testName = "Version";

        yield return CreateCommonLogging(testName, "2.0.0");
        yield return CreateNewtonsoftJson(testName, "12.0.2");
        yield return CreateOwin(testName, "1.0.0");
        yield return CreateStyleCopAnalyzers(testName, "1.1.118");

        yield return CreateFromXml("<version>1.0</version>", "1.0.0");
        yield return CreateFromXml("<version>9.2.0</version>", "9.2.0");
        yield return CreateFromXml("<version>9.2.0.0</version>", "9.2.0");
        yield return CreateFromXml("<version>9.2.0.1</version>", "9.2.0.1");

        yield return CreateFromXml("<version>1.0.0-alpha.1+githash</version>", "1.0.0-alpha.1");
    }

    private static IEnumerable<TestCaseData> GetDescriptionCases()
    {
        const string testName = "Description";
        yield return CreateCommonLogging(testName, "Common.Logging library introduces a simple abstraction to allow you to select a specific logging implementation at runtime.");
        yield return CreateNewtonsoftJson(testName, "Json.NET is a popular high-performance JSON framework for .NET");
        yield return CreateOwin(testName, "OWIN IAppBuilder startup interface");
        yield return CreateStyleCopAnalyzers(testName, "An implementation of StyleCop's rules using Roslyn analyzers and code fixes");
    }

    private static IEnumerable<TestCaseData> GetLicenseTypeCases()
    {
        const string testName = "LicenseType";
        yield return CreateCommonLogging(testName, (object?)null);
        yield return CreateNewtonsoftJson(testName, "expression");
        yield return CreateOwin(testName, (object?)null);
        yield return CreateStyleCopAnalyzers(testName, "expression");
    }

    private static IEnumerable<TestCaseData> GetLicenseValueCases()
    {
        const string testName = "LicenseValue";
        yield return CreateCommonLogging(testName, (object?)null);
        yield return CreateNewtonsoftJson(testName, "MIT");
        yield return CreateOwin(testName, (object?)null);
        yield return CreateStyleCopAnalyzers(testName, "Apache-2.0");
    }

    private static IEnumerable<TestCaseData> GetLicenseUrlCases()
    {
        const string testName = "LicenseUrl";
        yield return CreateCommonLogging(testName, (object?)null);
        yield return CreateNewtonsoftJson(testName, "https://licenses.nuget.org/MIT");
        yield return CreateOwin(testName, "https://github.com/owin-contrib/owin-hosting/blob/master/LICENSE.txt");
        yield return CreateStyleCopAnalyzers(testName, "https://licenses.nuget.org/Apache-2.0");
    }

    private static IEnumerable<TestCaseData> GetRepositoryUrlCases()
    {
        const string testName = "RepositoryUrl";
        yield return CreateCommonLogging(testName, (object?)null);
        yield return CreateNewtonsoftJson(testName, "https://github.com/JamesNK/Newtonsoft.Json");
        yield return CreateOwin(testName, (object?)null);
        yield return CreateStyleCopAnalyzers(testName, (object?)null);
    }

    private static IEnumerable<TestCaseData> GetProjectUrlCases()
    {
        const string testName = "ProjectUrl";
        yield return CreateCommonLogging(testName, "http://netcommon.sourceforge.net/");
        yield return CreateNewtonsoftJson(testName, "https://www.newtonsoft.com/json");
        yield return CreateOwin(testName, "https://github.com/owin-contrib/owin-hosting/");
        yield return CreateStyleCopAnalyzers(testName, "https://github.com/DotNetAnalyzers/StyleCopAnalyzers");
    }

    private static IEnumerable<TestCaseData> GetCopyrightCases()
    {
        const string testName = "Copyright";
        yield return CreateCommonLogging(testName, (object?)null);
        yield return CreateNewtonsoftJson(testName, "Copyright © James Newton-King 2008");
        yield return CreateOwin(testName, (object?)null);
        yield return CreateStyleCopAnalyzers(testName, "Copyright 2015 Tunnel Vision Laboratories, LLC");
    }

    private static IEnumerable<TestCaseData> GetAuthorCases()
    {
        const string testName = "Author";
        yield return CreateCommonLogging(testName, "Aleksandar Seovic, Mark Pollack, Erich Eichinger");
        yield return CreateNewtonsoftJson(testName, "James Newton-King");
        yield return CreateOwin(testName, "OWIN startup components contributors");
        yield return CreateStyleCopAnalyzers(testName, "Sam Harwell et. al.");
    }

    private static TestCaseData CreateCommonLogging(string testName, params object?[] args)
        => Create(testName, "Common.Logging.2.0.0", args);

    private static TestCaseData CreateNewtonsoftJson(string testName, params object?[] args)
        => Create(testName, "Newtonsoft.Json.12.0.2", args);

    private static TestCaseData CreateOwin(string testName, params object?[] args)
        => Create(testName, "Owin.1.0", args);

    private static TestCaseData CreateStyleCopAnalyzers(string testName, params object?[] args)
        => Create(testName, "StyleCop.Analyzers.1.1.118", args);

    private static TestCaseData Create(string testName, string fileName, object?[] args)
    {
        var resourceName = nameof(NuGetPackageSpecTest) + "." + fileName + ".nuspec.xml";

        NuGetPackageSpec spec;
        using (var stream = TempFile.OpenResource(typeof(NuGetPackageSpecTest), resourceName))
        {
            spec = NuGetPackageSpec.FromStream(stream);
        }

        var testArgs = new object?[args.Length + 1];
        testArgs[0] = spec;
        Array.Copy(args, 0, testArgs, 1, args.Length);

        return new TestCaseData(testArgs)
        {
            TestName = testName + "-" + fileName
        };
    }

    private static TestCaseData CreateFromXml(string metadata, params object?[] args)
    {
        var xml = new StringBuilder()
            .AppendLine("<package xmlns=\"http://schemas.microsoft.com/packaging/2013/01/nuspec.xsd\">")
            .AppendLine("<metadata>")
            .AppendLine(metadata)
            .AppendLine("</metadata>")
            .AppendLine("</package>");

        var spec = NuGetPackageSpec.FromStream(xml.ToString().AsStream());

        var testArgs = new object?[args.Length + 1];
        testArgs[0] = spec;
        Array.Copy(args, 0, testArgs, 1, args.Length);

        return new TestCaseData(testArgs)
        {
            TestName = metadata
        };
    }
}