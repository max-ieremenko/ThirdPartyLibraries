using System.IO;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Repository;

[TestFixture]
public class LibraryPathTest
{
    private TempFolder _location = null!;
    private DirectoryInfo _root = null!;

    [SetUp]
    public void BeforeEachTests()
    {
        _location = new TempFolder();
        _root = new DirectoryInfo(_location.Location);

        var path = _root.CreateSubdirectory(Path.Combine("nuget.org", "newtonsoft.json", "12.0.2"));
        File.WriteAllText(Path.Combine(path.FullName, "dummy.json"), string.Empty);

        path = _root.CreateSubdirectory(Path.Combine("nuget.org", "newtonsoft.json", "12.0.3"));
        File.WriteAllText(Path.Combine(path.FullName, "dummy.json"), string.Empty);

        path = _root.CreateSubdirectory(Path.Combine("npmjs.com", "@types", "angular", "1.6.51"));
        File.WriteAllText(Path.Combine(path.FullName, "dummy.json"), string.Empty);

        path = _root.CreateSubdirectory(Path.Combine("npmjs.com", "@types", "angular", "1.7.0"));
        File.WriteAllText(Path.Combine(path.FullName, "dummy.json"), string.Empty);
    }

    [TearDown]
    public void AfterEachTests()
    {
        _location.Dispose();
    }

    [Test]
    public void AsLibraryId()
    {
        var sut = new LibraryPath(_location.Location);

        var actual = sut
            .GetFiles("dummy.json")
            .Select(i => sut.AsLibraryId(i.Directory!))
            .ToArray();

        actual.ShouldBe(
            [
                new LibraryId("nuget.org", "newtonsoft.json", "12.0.2"),
                new LibraryId("nuget.org", "newtonsoft.json", "12.0.3"),
                new LibraryId("npmjs.com", "@types/angular", "1.7.0"),
                new LibraryId("npmjs.com", "@types/angular", "1.6.51")
            ],
            ignoreOrder: true);
    }

    [Test]
    public void RemoveLibraryNewtonsoft()
    {
        new LibraryPath(_root)
            .RemoveLibrary(new DirectoryInfo(Path.Combine(_location.Location, "nuget.org", "newtonsoft.json", "12.0.2")));

        Assert.That(Path.Combine(_location.Location, "nuget.org", "newtonsoft.json", "12.0.2"), Does.Not.Exist);
        Assert.That(Path.Combine(_location.Location, "nuget.org", "newtonsoft.json", "12.0.3"), Does.Exist);

        new LibraryPath(_root)
            .RemoveLibrary(new DirectoryInfo(Path.Combine(_location.Location, "nuget.org", "newtonsoft.json", "12.0.3")));

        Assert.That(Path.Combine(_location.Location, "nuget.org"), Does.Exist);
        Directory.GetFileSystemEntries(Path.Combine(_location.Location, "nuget.org")).ShouldBeEmpty();
    }

    [Test]
    public void RemoveLibraryAngular()
    {
        new LibraryPath(_root)
            .RemoveLibrary(new DirectoryInfo(Path.Combine(_location.Location, "npmjs.com", "@types/angular", "1.6.51")));

        Assert.That(Path.Combine(_location.Location, "npmjs.com", "@types", "angular", "1.7.0"), Does.Exist);
        Assert.That(Path.Combine(_location.Location, "npmjs.com", "@types", "angular", "1.6.51"), Does.Not.Exist);

        new LibraryPath(_root)
            .RemoveLibrary(new DirectoryInfo(Path.Combine(_location.Location, "npmjs.com", "@types/angular", "1.7.0")));

        Assert.That(Path.Combine(_location.Location, "npmjs.com"), Does.Exist);
        Directory.GetFileSystemEntries(Path.Combine(_location.Location, "npmjs.com")).ShouldBeEmpty();
    }
}