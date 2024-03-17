using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm.Internal;

[TestFixture]
public class NpmPackageSpecTest
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
    [TestCaseSource(nameof(GetLicenseCases))]
    public void GetLicense(IPackageSpec sut, PackageSpecLicenseType expectedType, string? expectedValue)
    {
        var actual = sut.ShouldBeOfType<NpmPackageSpec>().GetLicense();

        actual.Type.ShouldBe(expectedType);
        actual.Value.ShouldBe(expectedValue);
    }

    [Test]
    [TestCaseSource(nameof(GetRepositoryUrlCases))]
    public void GetRepositoryUrl(IPackageSpec sut, string? expected) => sut.ShouldBeOfType<NpmPackageSpec>().GetRepositoryUrl().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetHomePageCases))]
    public void GetHomePage(IPackageSpec sut, string? expected) => sut.ShouldBeOfType<NpmPackageSpec>().GetHomePage().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetAuthorCases))]
    public void GetAuthor(IPackageSpec sut, string? expected) => sut.GetAuthor().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetDependenciesCases))]
    public void GetDependencies(IPackageSpec sut, LibraryId[] expected) => sut.ShouldBeOfType<NpmPackageSpec>().GetDependencies().ShouldBe(expected);

    [Test]
    [TestCaseSource(nameof(GetDevDependenciesCases))]
    public void GetDevDependencies(IPackageSpec sut, LibraryId[] expected) => sut.ShouldBeOfType<NpmPackageSpec>().GetDevDependencies().ShouldBe(expected);

    private static IEnumerable<TestCaseData> GetNameCases()
    {
        const string testName = "Name";
        yield return CreateTypesAngular(testName, "@types/angular");
    }

    private static IEnumerable<TestCaseData> GetVersionCases()
    {
        const string testName = "Version";
        yield return CreateTypesAngular(testName, "1.6.55");
    }

    private static IEnumerable<TestCaseData> GetDescriptionCases()
    {
        const string testName = "Description";
        yield return CreateTypesAngular(testName, "TypeScript definitions for Angular JS");
    }

    // https://docs.npmjs.com/files/package.json
    private static IEnumerable<TestCaseData> GetLicenseCases()
    {
        const string testName = "License";
        yield return CreateTypesAngular(testName, PackageSpecLicenseType.Expression, "MIT");

        yield return CreateFromJson(testName, "simple", "{license: '(ISC OR GPL-3.0)'}", PackageSpecLicenseType.Expression, "(ISC OR GPL-3.0)");
        yield return CreateFromJson(testName, "file", "{license: 'SEE LICENSE IN license.txt'}", PackageSpecLicenseType.File, "license.txt");
        yield return CreateFromJson(testName, "no license", "{license: 'UNLICENSED'}", PackageSpecLicenseType.NotDefined, null);
        yield return CreateFromJson(testName, "object", "{license: { type: 'ISC' }}", PackageSpecLicenseType.Expression, "ISC");
        yield return CreateFromJson(testName, "array-1", "{licenses: [{ type: 'MIT' }]}", PackageSpecLicenseType.Expression, "MIT");
        yield return CreateFromJson(testName, "array-2", "{licenses: [{ type: 'MIT' }, { type: 'Apache-2.0' }]}", PackageSpecLicenseType.Expression, "(MIT OR Apache-2.0)");
    }

    private static IEnumerable<TestCaseData> GetRepositoryUrlCases()
    {
        const string testName = "RepositoryUrl";
        yield return CreateTypesAngular(testName, "https://github.com/DefinitelyTyped/DefinitelyTyped.git");

        yield return CreateFromJson(testName, "simple", "{repository: 'https://github.com'}", "https://github.com/");
        yield return CreateFromJson(testName, "with type", "{repository: { type: 'git', url: 'https://github.com'} }", "https://github.com/");
        yield return CreateFromJson(testName, "with prefix", "{repository: { type: 'git', url: 'git+https://github.com'} }", "https://github.com/");
        yield return CreateFromJson(testName, "short github", "{repository: 'github:user/repo'}", "https://github.com/user/repo");
        yield return CreateFromJson(testName, "short gitlab", "{repository: 'gitlab:user/repo'}", "https://gitlab.com/user/repo");
        yield return CreateFromJson(testName, "short bitbucket", "{repository: 'bitbucket:user/repo'}", "https://bitbucket.org/user/repo");
        yield return CreateFromJson(testName, "npm", "{repository: 'npm/npm'}", "https://github.com/npm/npm");
        yield return CreateFromJson(testName, "git schema", "{repository: 'git://github.com/pillarjs/hbs.git'}", "https://github.com/pillarjs/hbs.git");
        yield return CreateFromJson(testName, "not defined", "{ }", (object?)null);
    }

    private static IEnumerable<TestCaseData> GetHomePageCases()
    {
        const string testName = "HomePage";
        yield return CreateTypesAngular(testName, "https://github.com/DefinitelyTyped/DefinitelyTyped#readme");

        yield return CreateFromJson(testName, "not defined", "{ }", (object?)null);
    }

    private static IEnumerable<TestCaseData> GetAuthorCases()
    {
        const string testName = "Author";
        yield return CreateTypesAngular(testName, "Diego Vilar, Georgii Dolzhykov, Caleb St-Denis, Leonard Thieu, Steffen Kowalski");

        yield return CreateFromJson(
            testName,
            "person object",
            @"
{
author: {
    name: 'Barney Rubble',
    email: 'b@rubble.com',
    url: 'http://barnyrubble.tumblr.com'
}
}'",
            "Barney Rubble");

        yield return CreateFromJson(
            testName,
            "single string",
            @"
{
author: 'Barney Rubble <b@rubble.com> (http://barnyrubble.tumblr.com/)'
}'",
            "Barney Rubble");

        yield return CreateFromJson(
            testName,
            "array",
            @"
{
author: [
    {
        name: 'name 1'
    },
    {
        name: 'name 2'
    },
]
}'",
            "name 1, name 2");

        yield return CreateFromJson(testName, "not defined", "{ }", (object?)null);
    }

    private static IEnumerable<TestCaseData> GetDependenciesCases()
    {
        const string testName = "Dependencies";
        yield return CreateTypesAngular(
            testName,
            new[]
            {
                NpmLibraryId.New("angular", "^1.7.4"),
                NpmLibraryId.New("angular-animate", "^1.7.3")
            });
    }

    private static IEnumerable<TestCaseData> GetDevDependenciesCases()
    {
        const string testName = "DevDependencies";
        yield return CreateTypesAngular(
            testName,
            new[]
            {
                NpmLibraryId.New("@types/angular-animate", "^1.5.10"),
                NpmLibraryId.New("@types/angular-mocks", "^1.7.0"),
                NpmLibraryId.New("test", "*")
            });
    }

    private static TestCaseData CreateTypesAngular(string testName, params object?[] args)
        => Create(testName, "TypesAngular", args);

    private static TestCaseData Create(string testName, string fileName, object?[] args)
    {
        var resourceName = nameof(NpmPackageSpecTest) + "." + fileName + ".package.json";

        NpmPackageSpec spec;
        using (var stream = TempFile.OpenResource(typeof(NpmPackageSpecTest), resourceName))
        {
            spec = NpmPackageSpec.FromStream(stream);
        }

        var testArgs = new object?[args.Length + 1];
        testArgs[0] = spec;
        Array.Copy(args, 0, testArgs, 1, args.Length);

        return new TestCaseData(testArgs)
        {
            TestName = testName + "-" + fileName
        };
    }

    private static TestCaseData CreateFromJson(string testName, string jsonName, string json, params object?[] args)
    {
        var spec = new NpmPackageSpec(Encoding.UTF8.GetBytes(json).JsonDeserialize<JObject>());

        var testArgs = new object?[args.Length + 1];
        testArgs[0] = spec;
        Array.Copy(args, 0, testArgs, 1, args.Length);

        return new TestCaseData(testArgs)
        {
            TestName = testName + "-" + jsonName
        };
    }
}