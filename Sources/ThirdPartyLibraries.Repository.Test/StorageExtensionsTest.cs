using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository
{
    [TestFixture]
    public class StorageExtensionsTest
    {
        private Mock<IStorage> _storage;

        [SetUp]
        public void BeforeEachTest()
        {
            var fileContentByName = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            _storage = new Mock<IStorage>(MockBehavior.Strict);

            _storage
                .Setup(s => s.OpenLibraryFileReadAsync(It.IsAny<LibraryId>(), It.IsNotNull<string>(), CancellationToken.None))
                .Returns<LibraryId, string, CancellationToken>((id, fileName, _) =>
                {
                    var name = "{0}/{1}".FormatWith(id, fileName);
                    Stream result = null;
                    if (fileContentByName.TryGetValue(name, out var content))
                    {
                        result = new MemoryStream(content);
                    }

                    return Task.FromResult(result);
                });

            _storage
                .Setup(s => s.WriteLibraryFileAsync(It.IsAny<LibraryId>(), It.IsNotNull<string>(), It.IsNotNull<byte[]>(), CancellationToken.None))
                .Returns<LibraryId, string, byte[], CancellationToken>((id, fileName, content, _) =>
                {
                    var name = "{0}/{1}".FormatWith(id, fileName);
                    fileContentByName[name] = content;
                    return Task.CompletedTask;
                });

            _storage
                .Setup(s => s.OpenRootFileReadAsync(It.IsNotNull<string>(), CancellationToken.None))
                .Returns<string, CancellationToken>((fileName, _) =>
                {
                    Stream result = null;
                    if (fileContentByName.TryGetValue(fileName, out var content))
                    {
                        result = new MemoryStream(content);
                    }

                    return Task.FromResult(result);
                });

            _storage
                .Setup(s => s.WriteRootFileAsync(It.IsNotNull<string>(), It.IsNotNull<byte[]>(), CancellationToken.None))
                .Returns<string, byte[], CancellationToken>((fileName, content, _) =>
                {
                    fileContentByName[fileName] = content;
                    return Task.CompletedTask;
                });

            _storage
                .Setup(s => s.OpenConfigurationFileReadAsync(It.IsNotNull<string>(), CancellationToken.None))
                .Returns<string, CancellationToken>((fileName, _) =>
                {
                    Stream result = null;
                    if (fileContentByName.TryGetValue("configuration/" + fileName, out var content))
                    {
                        result = new MemoryStream(content);
                    }

                    return Task.FromResult(result);
                });

            _storage
                .Setup(s => s.CreateConfigurationFileAsync(It.IsNotNull<string>(), It.IsNotNull<byte[]>(), CancellationToken.None))
                .Returns<string, byte[], CancellationToken>((fileName, content, _) =>
                {
                    fileContentByName.Add("configuration/" + fileName, content);
                    return Task.CompletedTask;
                });
        }

        [Test]
        public async Task WriteReadLibraryIndexJson()
        {
            var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");
            var model = new NuGetLibraryIndexJson
            {
                License =
                {
                    Code = "MIT",
                    Status = "HasToBeApproved"
                }
            };

            await _storage.Object.WriteLibraryIndexJsonAsync(id, model, CancellationToken.None);
            var actual = await _storage.Object.ReadLibraryIndexJsonAsync<NuGetLibraryIndexJson>(id, CancellationToken.None);

            using (var stream = await _storage.Object.OpenLibraryFileReadAsync(id, StorageExtensions.IndexFileName, CancellationToken.None))
            {
                Console.WriteLine(new StreamReader(stream).ReadToEnd());
            }

            actual.ShouldNotBeNull();
            actual.License.Code.ShouldBe("MIT");
            actual.License.Status.ShouldBe("HasToBeApproved");
        }

        [Test]
        public async Task LibraryFileExists()
        {
            var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");

            var actual = await _storage.Object.LibraryFileExistsAsync(id, StorageExtensions.IndexFileName, CancellationToken.None);
            actual.ShouldBeFalse();

            await _storage.Object.WriteLibraryFileAsync(id, StorageExtensions.IndexFileName, Array.Empty<byte>(), CancellationToken.None);

            actual = await _storage.Object.LibraryFileExistsAsync(id, StorageExtensions.IndexFileName, CancellationToken.None);
            actual.ShouldBeTrue();
        }

        [Test]
        public async Task WriteReadCreateNewTemplate()
        {
            var context = new RootReadMeContext
            {
                Licenses =
                {
                    new RootReadMeLicenseContext
                    {
                        Code = "MIT",
                        LocalHRef = "licenses/mit",
                        RequiresApproval = false,
                        RequiresThirdPartyNotices = false,
                        PackagesCount = 1
                    }
                },
                Packages =
                {
                    new RootReadMePackageContext
                    {
                        Source = "nuget.org",
                        Name = "Newtonsoft.Json",
                        Version = "12.0.2",
                        License = "MIT",
                        LicenseLocalHRef = "licenses/mit",
                        UsedBy = "MyApp",
                        LocalHRef = "packages/nuget.org/newtonsoft.json/12.0.2",
                        SourceHRef = "https://www.nuget.org/packages/Newtonsoft.Json/12.0.2",
                        IsApproved = true
                    }
                }
            };

            await _storage.Object.WriteRootReadMeAsync(context, CancellationToken.None);

            var stream = await _storage.Object.OpenRootFileReadAsync(StorageExtensions.ReadMeFileName, CancellationToken.None);
            var content = (await stream.ToArrayAsync(CancellationToken.None)).AsText();
            Console.WriteLine(content);
            content.ShouldContain("Licenses");

            stream = await _storage.Object.OpenConfigurationFileReadAsync(StorageExtensions.ReadMeTemplateFileName, CancellationToken.None);
            stream.ShouldNotBeNull();
        }

        [Test]
        public async Task WriteReadUseExistingTemplate()
        {
            await _storage.Object.CreateConfigurationFileAsync(StorageExtensions.ReadMeTemplateFileName, "this is a template".AsBytes(), CancellationToken.None);
            
            await _storage.Object.WriteRootReadMeAsync(new RootReadMeContext(), CancellationToken.None);

            var stream = await _storage.Object.OpenRootFileReadAsync(StorageExtensions.ReadMeFileName, CancellationToken.None);
            var content = (await stream.ToArrayAsync(CancellationToken.None)).AsText();

            content.ShouldBe("this is a template");
        }
    }
}
