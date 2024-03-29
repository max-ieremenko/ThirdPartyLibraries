﻿using Moq;
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
            }
        };

        await _storage.Object.WriteLibraryIndexJsonAsync(id, model, CancellationToken.None).ConfigureAwait(false);
        var actual = await _storage.Object.ReadLibraryIndexJsonAsync<LibraryIndexJson>(id, CancellationToken.None).ConfigureAwait(false);

        using (var stream = await _storage.Object.OpenLibraryFileReadAsync(id, StorageExtensions.IndexFileName, CancellationToken.None).ConfigureAwait(false))
        {
            Console.WriteLine(new StreamReader(stream!).ReadToEnd());
        }

        actual.ShouldNotBeNull();
        actual.License.Code.ShouldBe("MIT");
        actual.License.Status.ShouldBe("HasToBeApproved");
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