using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository.Template;

namespace ThirdPartyLibraries.Repository;

[TestFixture]
public class FileStorageTest
{
    private TempFolder _location = null!;
    private FileStorage _sut = null!;

    [SetUp]
    public void BeforeEachTests()
    {
        _location = new TempFolder();
        _sut = new FileStorage(_location.Location);

        CopyFile(@"licenses\mit\index.json", "licenses.mit.index.json");

        CopyFile(@"packages\nuget.org\newtonsoft.json\12.0.2\index.json", "nuget.newtonsoft.json.index.json");
        CopyFile(@"packages\nuget.org\newtonsoft.json\12.0.2\package.nuspec", "nuget.newtonsoft.json.package.nuspec");
            
        CopyFile(@"packages\npmjs.com\@types\angular\1.6.51\index.json", "npmjs.types.angular.index.json");
        CopyFile(@"packages\npmjs.com\@types\angular\1.7.0\index.json", "npmjs.types.angular.index.json");
        CopyFile(@"packages\npmjs.com\angular\1.7.5\index.json", "npmjs.angular.index.json");
    }

    [TearDown]
    public void AfterEachTests()
    {
        _location.Dispose();
    }

    [Test]
    [TestCase("nuget.org", "newtonsoft.json", "12.0.2", null, null, null)]
    [TestCase("npmjs.com", "@types/angular", "1.6.51", null, null, null)]
    [TestCase("npmjs.com", "angular", "1.7.5", "npmjs.com", "@types/angular", "1.6.51")]
    [TestCase("npmjs.com", "@types/angular", "1.6.51", "npmjs.com", "angular", "1.7.5")]
    public void GetPackageLocalHRef(string librarySourceCode, string libraryName, string libraryVersion, string? relativeSourceCode, string relativeName, string relativeVersion)
    {
        var currentLocation = _location.Location;
        LibraryId? relativeTo = null;
        if (relativeSourceCode != null)
        {
            relativeTo = new LibraryId(relativeSourceCode, relativeName, relativeVersion);
            currentLocation = Path.Combine(currentLocation, "packages", relativeSourceCode, relativeName, relativeVersion);
        }

        Assert.That(currentLocation, Does.Exist.IgnoreFiles);

        var actual = _sut.GetPackageLocalHRef(new LibraryId(librarySourceCode, libraryName, libraryVersion), relativeTo);
        Console.WriteLine(actual);

        actual.ShouldBe(actual.ToLowerInvariant());

        var packageLocation = Path.Combine(currentLocation, actual);
        Assert.That(packageLocation, Does.Exist.IgnoreFiles);
        Assert.That(Path.Combine(packageLocation, "index.json"), Does.Exist.IgnoreDirectories);
    }

    [Test]
    [TestCase("MIT", null, null, null)]
    [TestCase("MIT", "nuget.org", "newtonsoft.json", "12.0.2")]
    [TestCase("MIT", "npmjs.com", "@types/angular", "1.6.51")]
    [TestCase("MIT", "npmjs.com", "angular", "1.7.5")]
    public void GetLicenseLocalHRef(string licenseCode, string? librarySourceCode, string libraryName, string libraryVersion)
    {
        var currentLocation = _location.Location;
        LibraryId? relativeTo = null;
        if (librarySourceCode != null)
        {
            relativeTo = new LibraryId(librarySourceCode, libraryName, libraryVersion);
            currentLocation = Path.Combine(currentLocation, "packages", librarySourceCode, libraryName, libraryVersion);
        }

        Assert.That(currentLocation, Does.Exist.IgnoreFiles);

        var actual = _sut.GetLicenseLocalHRef(licenseCode, relativeTo);
        Console.WriteLine(actual);

        actual.ShouldBe(actual.ToLowerInvariant());

        var licenseLocation = Path.Combine(currentLocation, actual);
        Assert.That(licenseLocation, Does.Exist.IgnoreFiles);
        Assert.That(Path.Combine(licenseLocation, "index.json"), Does.Exist.IgnoreDirectories);
    }

    [Test]
    public async Task GetAllLibraries()
    {
        var actual = await _sut.GetAllLibrariesAsync(default).ConfigureAwait(false);

        actual.ShouldBe(
            new[]
            {
                new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2"),
                new LibraryId("npmjs.com", "@types/angular", "1.6.51"),
                new LibraryId("npmjs.com", "@types/angular", "1.7.0"),
                new LibraryId("npmjs.com", "angular", "1.7.5")
            },
            ignoreOrder: true);
    }

    [Test]
    [TestCase("nuget.org", "Newtonsoft.Json", new[] { "12.0.2" })]
    [TestCase("npmjs.com", "@types/angular", new[] { "1.6.51", "1.7.0" })]
    [TestCase("npmjs.com", "angular-unknown", new string[0])]
    public async Task GetAllLibraryVersionsAsync(string sourceCode, string name, string[] expected)
    {
        var actual = await _sut.GetAllLibraryVersionsAsync(sourceCode, name, default).ConfigureAwait(false);
        
        if (expected.Length == 0)
        {
            actual.ShouldBeEmpty();
        }
        else
        {
            var expectedList = expected.Select(i => new LibraryId(sourceCode, name, i)).ToArray();
            actual.ShouldBe(expectedList, ignoreOrder: true);
        }
    }

    [Test]
    public async Task GetAllLicenseCodes()
    {
        var actual = await _sut.GetAllLicenseCodesAsync(default).ConfigureAwait(false);

        actual.ShouldBe(new[] { "mit" });
    }

    [Test]
    [TestCase("nuget.org", "Newtonsoft.Json", "12.0.2", "package.nuspec")]
    [TestCase("npmjs.com", "@types/angular", "1.6.51", "index.json")]
    [TestCase("npmjs.com", "@types/angular", "1.7.0", "index.json")]
    public async Task OpenLibraryFileRead(string sourceCode, string name, string version, string fileName)
    {
        var id = new LibraryId(sourceCode, name, version);

        var actual = await _sut.OpenLibraryFileReadAsync(id, fileName, default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        using (actual)
        {
            new StreamReader(actual).ReadToEnd();
        }
    }

    [Test]
    public async Task OpenLibraryFileReadNotExists()
    {
        var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");

        var actual = await _sut.OpenLibraryFileReadAsync(id, "package1.nuspec", default).ConfigureAwait(false);

        actual.ShouldBeNull();
    }

    [Test]
    public async Task WriteReadLibraryFileNew()
    {
        var id = new LibraryId("nuget.org", "Newtonsoft.Json", "1.0.0");

        await _sut.WriteLibraryFileAsync(id, "readme.md", "some text".AsBytes(), default).ConfigureAwait(false);
        var actual = await _sut.OpenLibraryFileReadAsync(id, "readme.md", default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        using (actual)
        {
            new StreamReader(actual).ReadToEnd().ShouldBe("some text");
        }
    }

    [Test]
    public async Task WriteReadLibraryFileExisting()
    {
        var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");

        await _sut.WriteLibraryFileAsync(id, "package.nuspec", "some text".AsBytes(), default).ConfigureAwait(false);
        var actual = await _sut.OpenLibraryFileReadAsync(id, "package.nuspec", default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        using (actual)
        {
            new StreamReader(actual).ReadToEnd().ShouldBe("some text");
        }
    }

    [Test]
    public async Task LoadUnknownLicense()
    {
        var actual = await _sut.ReadLicenseIndexJsonAsync("some code", default).ConfigureAwait(false);

        actual.ShouldBeNull();
    }

    [Test]
    public void TryToUpdateLicense()
    {
        var model = new LicenseIndexJson
        {
            Code = "MIT",
            RequiresApproval = true,
            HRef = "link"
        };

        Assert.ThrowsAsync<NotSupportedException>(() => _sut.CreateLicenseIndexJsonAsync(model, default));
    }

    [Test]
    public async Task CreateLoadLicense()
    {
        var model = new LicenseIndexJson
        {
            Code = "Apache-2.0",
            RequiresApproval = true,
            HRef = "link",
            FileName = "file name.md"
        };

        await _sut.CreateLicenseIndexJsonAsync(model, default).ConfigureAwait(false);
        var actual = await _sut.ReadLicenseIndexJsonAsync(model.Code, default).ConfigureAwait(false);

        actual.ShouldNotBeNull();
        actual.Code.ShouldBe(model.Code);
        actual.RequiresApproval.ShouldBe(model.RequiresApproval);
        actual.HRef.ShouldBe(model.HRef);
        actual.FileName.ShouldBe("file name.md");
    }

    [Test]
    public async Task RemoveLibraryNewtonsoft()
    {
        var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");
        Assert.That(_sut.GetPackageLocation(id), Does.Exist.IgnoreFiles);

        await _sut.RemoveLibraryAsync(id, default).ConfigureAwait(false);

        Assert.That(Path.GetDirectoryName(_sut.GetPackageLocation(id)), Does.Not.Exist);
    }

    [Test]
    public async Task RemoveLibraryTypesAngular()
    {
        var id = new LibraryId("npmjs.com", "@types/angular", "1.6.51");
        Assert.That(_sut.GetPackageLocation(id), Does.Exist.IgnoreFiles);

        await _sut.RemoveLibraryAsync(id, default).ConfigureAwait(false);

        Assert.That(Path.Combine(_location.Location, "packages", "npmjs.com", "@types"), Does.Exist);

        id = new LibraryId("npmjs.com", "@types/angular", "1.7.0");
        Assert.That(_sut.GetPackageLocation(id), Does.Exist.IgnoreFiles);

        await _sut.RemoveLibraryAsync(id, default).ConfigureAwait(false);

        Assert.That(Path.Combine(_location.Location, "packages", "npmjs.com"), Does.Exist);
        Assert.That(Path.Combine(_location.Location, "packages", "npmjs.com", "@types"), Does.Not.Exist);
    }

    [Test]
    public async Task RemoveUnknownLibrary()
    {
        var id = new LibraryId("nuget.org", "some name", "version");

        await _sut.RemoveLibraryAsync(id, default).ConfigureAwait(false);
    }

    [Test]
    [TestCase("index.json", "index.json")]
    [TestCase("INDEX.json", "index.json")]
    [TestCase("*.json", "index.json")]
    [TestCase("*.*", "index.json", "package.nuspec")]
    public async Task FindLibraryFiles(string searchPattern, params string[] expected)
    {
        var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");

        var actual = await _sut.FindLibraryFilesAsync(id, searchPattern, default).ConfigureAwait(false);

        actual.ShouldBe(expected, ignoreOrder: true);
    }

    private void CopyFile(string targetName, string resourceName)
    {
        targetName = targetName.Replace('\\', Path.DirectorySeparatorChar);

        var fileName = Path.Combine(_location.Location, targetName);
        Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);

        resourceName = GetType().Namespace + ".Storage." + resourceName;

        using (var dest = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite))
        using (var source = GetType().Assembly.GetManifestResourceStream(resourceName))
        {
            source!.CopyTo(dest);
        }
    }
}