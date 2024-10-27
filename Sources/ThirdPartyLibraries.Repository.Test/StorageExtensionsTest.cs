using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository;

[TestFixture]
public class StorageExtensionsTest
{
    private Mock<IStorage> _storage = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        var fileContentByName = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        _storage = new Mock<IStorage>(MockBehavior.Strict);

        _storage
            .Setup(s => s.OpenLibraryFileReadAsync(It.IsAny<LibraryId>(), It.IsNotNull<string>(), CancellationToken.None))
            .Returns<LibraryId, string, CancellationToken>((id, fileName, _) =>
            {
                var name = $"{id}/{fileName}";
                Stream? result = null;
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
                var name = $"{id}/{fileName}";
                fileContentByName[name] = content;
                return Task.CompletedTask;
            });

        _storage
            .Setup(s => s.OpenRootFileReadAsync(It.IsNotNull<string>(), CancellationToken.None))
            .Returns<string, CancellationToken>((fileName, _) =>
            {
                Stream? result = null;
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
                Stream? result = null;
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
        var model = new LibraryIndexJson
        {
            License =
            {
                Code = "MIT",
                Status = "HasToBeApproved"
            },
            Licenses =
            {
                new LibraryLicense
                {
                    Subject = "subject",
                    Code = "MIT",
                    HRef = "href",
                    Description = "description"
                }
            },
            UsedBy =
            {
                new Application
                {
                    Name = "app",
                    InternalOnly = true,
                    TargetFrameworks = ["1", "2"],
                    Dependencies =
                    [
                        new LibraryDependency
                        {
                            Name = "name",
                            Version = "version"
                        }
                    ]
                }
            }
        };

        await _storage.Object.WriteLibraryIndexJsonAsync(id, model, CancellationToken.None).ConfigureAwait(false);
        var actual = await _storage.Object.ReadLibraryIndexJsonAsync(id, CancellationToken.None).ConfigureAwait(false);

        using (var stream = await _storage.Object.OpenLibraryFileReadAsync(id, StorageExtensions.IndexFileName, CancellationToken.None).ConfigureAwait(false))
        {
            Console.WriteLine(new StreamReader(stream!).ReadToEnd());
        }

        actual.ShouldNotBeNull();
        actual.License.Code.ShouldBe("MIT");
        actual.License.Status.ShouldBe("HasToBeApproved");
        
        actual.Licenses.Count.ShouldBe(1);
        actual.Licenses[0].Subject.ShouldBe("subject");
        actual.Licenses[0].Code.ShouldBe("MIT");
        actual.Licenses[0].HRef.ShouldBe("href");
        actual.Licenses[0].Description.ShouldBe("description");

        actual.UsedBy.Count.ShouldBe(1);
        actual.UsedBy[0].Name.ShouldBe("app");
        actual.UsedBy[0].InternalOnly.ShouldBeTrue();
        actual.UsedBy[0].TargetFrameworks.ShouldBe(["1", "2"]);

        actual.UsedBy[0].Dependencies?.Length.ShouldBe(1);
        actual.UsedBy[0].Dependencies![0].Name.ShouldBe("name");
        actual.UsedBy[0].Dependencies![0].Version.ShouldBe("version");
    }

    [Test]
    public void FixLineEnding()
    {
        var data = @"
line 1

line 2
".Replace("\r\n", "\n");

        var actual1 = StorageExtensions.FixLineEnding(data.AsStream());
        var actual2 = StorageExtensions.FixLineEnding(data.Replace("\n", "\r\n").AsStream());
        actual1.ShouldBe(actual2);

        var text = Encoding.UTF8.GetString(actual1);
        text.ShouldBe(data.Replace("\n", "\r\n"));
    }

    [Test]
    public async Task LibraryFileExists()
    {
        var id = new LibraryId("nuget.org", "Newtonsoft.Json", "12.0.2");

        var actual = await _storage.Object.LibraryFileExistsAsync(id, StorageExtensions.IndexFileName, CancellationToken.None).ConfigureAwait(false);
        actual.ShouldBeFalse();

        await _storage.Object.WriteLibraryFileAsync(id, StorageExtensions.IndexFileName, Array.Empty<byte>(), CancellationToken.None).ConfigureAwait(false);

        actual = await _storage.Object.LibraryFileExistsAsync(id, StorageExtensions.IndexFileName, CancellationToken.None).ConfigureAwait(false);
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

        await _storage.Object.WriteRootReadMeAsync(context, CancellationToken.None).ConfigureAwait(false);

        var stream = await _storage.Object.OpenRootFileReadAsync(StorageExtensions.ReadMeFileName, CancellationToken.None).ConfigureAwait(false);
        stream.ShouldNotBeNull();

        var content = (await stream.ToArrayAsync(CancellationToken.None).ConfigureAwait(false)).AsText();
        Console.WriteLine(content);
        content.ShouldContain("Licenses");

        stream = await _storage.Object.OpenConfigurationFileReadAsync(StorageExtensions.ReadMeTemplateFileName, CancellationToken.None).ConfigureAwait(false);
        stream.ShouldNotBeNull();
    }

    [Test]
    public async Task WriteReadUseExistingTemplate()
    {
        await _storage.Object.CreateConfigurationFileAsync(StorageExtensions.ReadMeTemplateFileName, "this is a template".AsBytes(), CancellationToken.None).ConfigureAwait(false);
            
        await _storage.Object.WriteRootReadMeAsync(new RootReadMeContext(), CancellationToken.None).ConfigureAwait(false);

        var stream = await _storage.Object.OpenRootFileReadAsync(StorageExtensions.ReadMeFileName, CancellationToken.None).ConfigureAwait(false);
        stream.ShouldNotBeNull();

        var content = (await stream.ToArrayAsync(CancellationToken.None).ConfigureAwait(false)).AsText();

        content.ShouldBe("this is a template");
    }
}