using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm;

[TestFixture]
public class PackageJsonParserTest
{
    private PackageJsonParser _sut;

    [SetUp]
    public void BeforeEachTest()
    {
        using (var stream = TempFile.OpenResource(GetType(), "PackageJsonParserTest.TypesAngular.package.json"))
        {
            _sut = PackageJsonParser.FromStream(stream);
        }
    }

    [Test]
    public void GetName()
    {
        _sut.GetName().ShouldBe("@types/angular");
    }

    [Test]
    public void GetVersion()
    {
        _sut.GetVersion().ShouldBe("1.6.55");
    }

    [Test]
    public void GetDependencies()
    {
        _sut.GetDependencies().ShouldBe(new[]
        {
            new NpmPackageId("angular", "^1.7.4"),
            new NpmPackageId("angular-animate", "^1.7.3")
        });
    }

    [Test]
    public void GetDevDependencies()
    {
        _sut.GetDevDependencies().ShouldBe(new[]
        {
            new NpmPackageId("@types/angular-animate", "^1.5.10"),
            new NpmPackageId("@types/angular-mocks", "^1.7.0"),
            new NpmPackageId("test", "*")
        });
    }

    // https://docs.npmjs.com/files/package.json
    [Test]
    [TestCase("{license: 'MIT'}", "expression", "MIT")]
    [TestCase("{license: '(ISC OR GPL-3.0)'}", "expression", "(ISC OR GPL-3.0)")]
    [TestCase("{license: 'SEE LICENSE IN license.txt'}", "file", "license.txt")]
    [TestCase("{license: 'UNLICENSED'}", "expression", null)]
    [TestCase("{license: { type: 'ISC' }}", "expression", "ISC")]
    [TestCase("{licenses: [{ type: 'MIT' }]}", "expression", "MIT")]
    [TestCase("{licenses: [{ type: 'MIT' }, { type: 'Apache-2.0' }]}", "expression", "(MIT OR Apache-2.0)")]
    public void GetLicense(string json, string expectedType, string expectedValue)
    {
        _sut = new PackageJsonParser(Encoding.UTF8.GetBytes(json).JsonDeserialize<JObject>());
            
        var actual = _sut.GetLicense();

        actual.Type.ShouldBe(expectedType);
        actual.Value.ShouldBe(expectedValue);
    }

    [Test]
    [TestCase("{repository: 'https://github.com'}", null, "https://github.com/")]
    [TestCase("{repository: { type: 'git', url: 'https://github.com'} }", "git", "https://github.com/")]
    [TestCase("{repository: { type: 'git', url: 'git+https://github.com'} }", "git", "https://github.com/")]
    [TestCase("{repository: 'github:user/repo'}", "git", "https://github.com/user/repo")]
    [TestCase("{repository: 'gitlab:user/repo'}", null, "https://gitlab.com/user/repo")]
    [TestCase("{repository: 'bitbucket:user/repo'}", null, "https://bitbucket.org/user/repo")]
    [TestCase("{repository: 'bitbucket:user/repo'}", null, "https://bitbucket.org/user/repo")]
    [TestCase("{repository: 'npm/npm'}", "git", "https://github.com/npm/npm")]
    [TestCase("{repository: 'npm/npm'}", "git", "https://github.com/npm/npm")]
    [TestCase("{repository: 'git://github.com/pillarjs/hbs.git'}", "git", "https://github.com/pillarjs/hbs.git")]
    [TestCase("{ }", null, null)]
    public void GetRepository(string json, string expectedType, string expectedUrl)
    {
        _sut = new PackageJsonParser(Encoding.UTF8.GetBytes(json).JsonDeserialize<JObject>());

        var actual = _sut.GetRepository();

        if (expectedUrl == null)
        {
            actual.ShouldBeNull();
        }
        else
        {
            actual.ShouldNotBeNull();
            actual.Type.ShouldBe(expectedType);
            actual.Url.ShouldBe(expectedUrl);
        }
    }

    [Test]
    [TestCase("{author: {name: 'The name'}}", "The name")]
    [TestCase("{author: 'The name <email>'}", "The name <email>")]
    [TestCase("{contributors: [{name: 'The name1'}, {name: 'The name2'}]}", "The name1, The name2")]
    [TestCase("{contributors: ['The name1 <email1>', 'The name2 <email2>']}", "The name1 <email1>, The name2 <email2>")]
    [TestCase("{}", null)]
    public void GetAuthors(string json, string expected)
    {
        _sut = new PackageJsonParser(Encoding.UTF8.GetBytes(json).JsonDeserialize<JObject>());

        var actual = _sut.GetAuthors();

        actual.ShouldBe(expected);
    }
}