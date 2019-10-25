using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository
{
    [TestFixture]
    public class FileStorageTest
    {
        private TempFolder _location;
        private FileStorage _sut;

        [SetUp]
        public void BeforeEachTests()
        {
            _location = new TempFolder();
            _sut = new FileStorage(_location.Location);

            foreach (var resourceName in GetType().Assembly.GetManifestResourceNames())
            {
                if (TryParseNuGetFileName(resourceName, out var fileName)
                || TryParseLicenseFileName(resourceName, out fileName))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    using (var dest = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite))
                    using (var source = GetType().Assembly.GetManifestResourceStream(resourceName))
                    {
                        source.CopyTo(dest);
                    }
                }
            }
        }

        [TearDown]
        public void AfterEachTests()
        {
            _location?.Dispose();
        }

        [Test]
        [TestCase(PackageSources.NuGet, "Castle.Core", "4.4.0", RelativeTo.Root, "packages/nuget.org/castle.core/4.4.0")]
        [TestCase(PackageSources.NuGet, "Castle.Core", "4.4.0", RelativeTo.Library, "../../../../packages/nuget.org/castle.core/4.4.0")]
        public void GetPackageLocalHRef(string sourceCode, string name, string version, RelativeTo relativeTo, string expected)
        {
            var id = new LibraryId(sourceCode, name, version);

            var actual = _sut.GetPackageLocalHRef(id, relativeTo);

            Console.WriteLine(actual);
            actual.ShouldBe(expected);
        }

        [Test]
        [TestCase("MICROSOFT .NET LIBRARY", RelativeTo.Root, "licenses/microsoft .net library")]
        [TestCase("MICROSOFT .NET LIBRARY", RelativeTo.Library, "../../../../licenses/microsoft .net library")]
        public void GetLicenseLocalHRef(string code, RelativeTo relativeTo, string expected)
        {
            var actual = _sut.GetLicenseLocalHRef(code, relativeTo);

            Console.WriteLine(actual);
            actual.ShouldBe(expected);
        }

        [Test]
        public async Task GetAllLibraries()
        {
            var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");

            var actual = await _sut.GetAllLibrariesAsync(CancellationToken.None);

            actual.Count.ShouldBe(1);
            actual[0].ShouldBe(id);
        }

        [Test]
        public async Task OpenLibraryFileRead()
        {
            var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");

            var actual = await _sut.OpenLibraryFileReadAsync(id, "package.nuspec", CancellationToken.None);

            actual.ShouldNotBeNull();
            using (actual)
            {
                var text = new StreamReader(actual).ReadToEnd();
                text.ShouldContain("<id>Newtonsoft.Json</id>");
            }
        }

        [Test]
        public async Task OpenLibraryFileReadNotExists()
        {
            var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");

            var actual = await _sut.OpenLibraryFileReadAsync(id, "package1.nuspec", CancellationToken.None);

            actual.ShouldBeNull();
        }

        [Test]
        public async Task WriteReadLibraryFileNew()
        {
            var id = new LibraryId("nuget.org", "Newtonsoft.Json", "1.0.0");

            await _sut.WriteLibraryFileAsync(id, "readme.md", "some text".AsBytes(), CancellationToken.None);
            var actual = await _sut.OpenLibraryFileReadAsync(id, "readme.md", CancellationToken.None);

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

            await _sut.WriteLibraryFileAsync(id, "package.nuspec", "some text".AsBytes(), CancellationToken.None);
            var actual = await _sut.OpenLibraryFileReadAsync(id, "package.nuspec", CancellationToken.None);

            actual.ShouldNotBeNull();
            using (actual)
            {
                new StreamReader(actual).ReadToEnd().ShouldBe("some text");
            }
        }

        [Test]
        public async Task LoadUnknownLicense()
        {
            var actual = await _sut.ReadLicenseIndexJsonAsync("some code", CancellationToken.None);

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

            Assert.ThrowsAsync<NotSupportedException>(() => _sut.CreateLicenseIndexJsonAsync(model, CancellationToken.None));
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

            await _sut.CreateLicenseIndexJsonAsync(model, CancellationToken.None);
            var actual = await _sut.ReadLicenseIndexJsonAsync(model.Code, CancellationToken.None);

            actual.ShouldNotBeNull();
            actual.Code.ShouldBe(model.Code);
            actual.RequiresApproval.ShouldBe(model.RequiresApproval);
            actual.HRef.ShouldBe(model.HRef);
            actual.FileName.ShouldBe("file name.md");
        }

        [Test]
        public async Task RemoveLibrary()
        {
            var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");
            DirectoryAssert.Exists(_sut.GetPackageLocation(id));

            await _sut.RemoveLibraryAsync(id, CancellationToken.None);

            DirectoryAssert.DoesNotExist(Path.GetDirectoryName(_sut.GetPackageLocation(id)));
        }

        [Test]
        public async Task RemoveUnknownLibrary()
        {
            var id = new LibraryId("nuget.org", "some name", "version");

            await _sut.RemoveLibraryAsync(id, CancellationToken.None);
        }

        private bool TryParseNuGetFileName(string resourceName, out string fileName)
        {
            fileName = default;

            var anchor = GetType().Namespace + ".Storage.packages.nuget.org.";
            if (!resourceName.StartsWithIgnoreCase(anchor))
            {
                return false;
            }

            var name = resourceName.AsSpan().Slice(anchor.Length);

            var versionIndex = name.FindIndex(char.IsDigit);
            var packageName = name.Slice(0, versionIndex - 2).ToString();
            name = name.Slice(versionIndex);

            versionIndex = name.FindLastIndex(char.IsDigit);
            var version = name.Slice(0, versionIndex + 1).ToString().Replace("_", string.Empty);

            fileName = name.Slice(versionIndex + 2).ToString();

            fileName = Path.Combine(_location.Location, @"packages\nuget.org", packageName, version, fileName);
            return true;
        }

        private bool TryParseLicenseFileName(string resourceName, out string fileName)
        {
            fileName = default;

            var anchor = GetType().Namespace + ".Storage.licenses.";
            if (!resourceName.StartsWithIgnoreCase(anchor))
            {
                return false;
            }

            var name = resourceName.AsSpan().Slice(anchor.Length);

            var index = name.IndexOf("index.json");
            
            var code = name.Slice(0, index - 1).ToString();
            fileName = name.Slice(index).ToString();

            fileName = Path.Combine(_location.Location, "licenses", code, fileName);
            return true;
        }
    }
}
