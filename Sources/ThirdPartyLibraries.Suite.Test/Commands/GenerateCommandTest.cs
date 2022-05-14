using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands;

[TestFixture]
public class GenerateCommandTest
{
    private const string AppName = "App";

    private GenerateCommand _sut;
    private Mock<ILogger> _logger;
    private TempFolder _to;
    private Mock<IPackageRepository> _packageRepository;
    private Mock<IStorage> _storage;
    private IList<Package> _packages;
    private IServiceProvider _serviceProvider;

    [SetUp]
    public void BeforeEachTest()
    {
        _logger = new Mock<ILogger>(MockBehavior.Loose);
            
        _to = new TempFolder();
        Directory.CreateDirectory(Path.Combine(_to.Location, "configuration"));

        _packages = new List<Package>();

        _storage = new Mock<IStorage>(MockBehavior.Strict);
        _storage
            .SetupGet(s => s.ConnectionString)
            .Returns("some path");
        _storage
            .Setup(s => s.OpenConfigurationFileReadAsync(It.IsNotNull<string>(), CancellationToken.None))
            .Returns<string, CancellationToken>((fileName, _) =>
            {
                var path = Path.Combine(_to.Location, "configuration", fileName);
                Stream result = File.Exists(path) ? File.OpenRead(path) : null;
                return Task.FromResult(result);
            });
        _storage
            .Setup(s => s.CreateConfigurationFileAsync(It.IsNotNull<string>(), It.IsNotNull<byte[]>(), CancellationToken.None))
            .Returns<string, byte[], CancellationToken>((fileName, content, _) =>
            {
                var path = Path.Combine(_to.Location, "configuration", fileName);
                File.WriteAllBytes(path, content);
                return Task.CompletedTask;
            });
        _storage
            .Setup(s => s.GetAllLibrariesAsync(CancellationToken.None))
            .ReturnsAsync(() => _packages.Select(i => new LibraryId(PackageSources.NuGet, i.Name, i.Version)).ToArray());

        _packageRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
        _packageRepository
            .SetupGet(r => r.Storage)
            .Returns(_storage.Object);
        _packageRepository
            .Setup(r => r.LoadPackageAsync(It.IsAny<LibraryId>(), CancellationToken.None))
            .Returns<LibraryId, CancellationToken>((id, _) =>
            {
                var package = _packages.Single(i => i.Name == id.Name && i.Version == id.Version);
                return Task.FromResult(package);
            });

        var services = new ServiceCollection();
        services.AddSingleton(_packageRepository.Object);
        services.AddSingleton(_logger.Object);
        _serviceProvider = services.BuildServiceProvider();

        _sut = new GenerateCommand
        {
            To = _to.Location,
            AppNames = { AppName }
        };
    }

    [TearDown]
    public void AfterEachTest()
    {
        _to?.Dispose();
    }

    [Test]
    public async Task GenerateThirdPartyNoticesWithRepositoryLicenses()
    {
        var package = new Package
        {
            SourceCode = PackageSources.NuGet,
            Name = "Newtonsoft.Json",
            Version = "12.0.2",
            LicenseCode = "MIT",
            HRef = "https://www.nuget.org/packages/Newtonsoft.Json/12.0.2",
            Author = "James Newton-King",
            Copyright = "Copyright © James Newton-King 2008",
            UsedBy = new[] { PackageApplication.Public(AppName) },
            ThirdPartyNotices = "some extra notices"
        };
        _packages.Add(package);

        var license = new LicenseIndexJson
        {
            Code = "MIT",
            HRef = "https://spdx.org/licenses/MIT.html",
            FileName = "license.txt",
            FullName = "MIT License"
        };

        _storage
            .Setup(s => s.OpenLicenseFileReadAsync("MIT", "index.json", CancellationToken.None))
            .ReturnsAsync(license.JsonSerialize);

        _storage
            .Setup(s => s.OpenLicenseFileReadAsync("MIT", "license.txt", CancellationToken.None))
            .ReturnsAsync(() => new MemoryStream("MIT license text".AsBytes()));

        UpdateTemplate("with-lic-files.txt");

        await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

        FileAssert.Exists(Path.Combine(_to.Location, GenerateCommand.OutputFileName));
        FileAssert.Exists(Path.Combine(_to.Location, "Licenses", "MIT-license.txt"));

        var output = File.ReadAllText(Path.Combine(_to.Location, GenerateCommand.OutputFileName));
        output.ShouldContain(package.Name);
        output.ShouldContain(package.Version);
        output.ShouldContain(package.HRef);
        output.ShouldContain(package.Author);
        output.ShouldContain(package.Copyright);
        output.ShouldContain(package.ThirdPartyNotices);

        output.ShouldContain(license.HRef);
        output.ShouldContain(license.FileName);
        output.ShouldContain(license.FullName);

        output = File.ReadAllText(Path.Combine(_to.Location, "Licenses", "MIT-license.txt"));
        output.ShouldBe("MIT license text");
    }

    [Test]
    public async Task GenerateThirdPartyNoticesWithHRefsOnly()
    {
        var package = new Package
        {
            SourceCode = PackageSources.NuGet,
            Name = "Newtonsoft.Json",
            Version = "12.0.2",
            LicenseCode = "MIT",
            HRef = "https://www.nuget.org/packages/Newtonsoft.Json/12.0.2",
            Author = "James Newton-King",
            Copyright = "Copyright © James Newton-King 2008",
            UsedBy = new[] { PackageApplication.Public(AppName) },
            ThirdPartyNotices = "some extra notices"
        };
        _packages.Add(package);

        var license = new LicenseIndexJson
        {
            Code = "MIT",
            HRef = "https://spdx.org/licenses/MIT.html",
            FileName = "license.txt",
            FullName = "MIT License"
        };

        _storage
            .Setup(s => s.OpenLicenseFileReadAsync("MIT", "index.json", CancellationToken.None))
            .ReturnsAsync(license.JsonSerialize);

        _storage
            .Setup(s => s.OpenLicenseFileReadAsync("MIT", "license.txt", CancellationToken.None))
            .ReturnsAsync("MIT license text".AsStream);

        UpdateTemplate("with-lic-hrefs.txt");

        await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

        FileAssert.Exists(Path.Combine(_to.Location, GenerateCommand.OutputFileName));
        DirectoryAssert.DoesNotExist(Path.Combine(_to.Location, "Licenses"));

        var output = File.ReadAllText(Path.Combine(_to.Location, GenerateCommand.OutputFileName));
        output.ShouldContain(package.Name);
        output.ShouldContain(package.Version);
        output.ShouldContain(package.HRef);
        output.ShouldContain(package.Author);
        output.ShouldContain(package.Copyright);
        output.ShouldContain(package.ThirdPartyNotices);

        output.ShouldContain(license.HRef);
        output.ShouldContain(license.FullName);
        output.ShouldNotContain(license.FileName);
    }

    [Test]
    public async Task GenerateThirdPartyNoticesWithPackageLicenses()
    {
        var package = new Package
        {
            SourceCode = PackageSources.NuGet,
            Name = "Newtonsoft.Json",
            Version = "12.0.2",
            LicenseCode = "MIT",
            HRef = "https://www.nuget.org/packages/Newtonsoft.Json/12.0.2",
            Author = "James Newton-King",
            Copyright = "Copyright © James Newton-King 2008",
            UsedBy = new[] { PackageApplication.Public(AppName) },
            ThirdPartyNotices = "some extra notices",
            Licenses =
            {
                new PackageLicense
                {
                    Code = "MIT",
                    HRef = "https://licenses.nuget.org/MIT",
                    Subject = PackageLicense.SubjectPackage
                }
            }
        };
        _packages.Add(package);

        var license = new LicenseIndexJson
        {
            Code = "MIT",
            HRef = "https://spdx.org/licenses/MIT.html",
            FileName = "license.txt",
            FullName = "MIT License"
        };

        _storage
            .Setup(s => s.OpenLicenseFileReadAsync("MIT", "index.json", CancellationToken.None))
            .ReturnsAsync(license.JsonSerialize);

        _storage
            .Setup(s => s.OpenLicenseFileReadAsync("MIT", "license.txt", CancellationToken.None))
            .ReturnsAsync("MIT license text".AsStream);

        _storage
            .Setup(s => s.FindLibraryFilesAsync(new LibraryId(package.SourceCode, package.Name, package.Version), "package-*lic*", CancellationToken.None))
            .ReturnsAsync(() => new[] { "package-license.md" });

        _storage
            .Setup(s => s.OpenLibraryFileReadAsync(new LibraryId(package.SourceCode, package.Name, package.Version), "package-license.md", CancellationToken.None))
            .ReturnsAsync(() => new MemoryStream("newtonsoft license text".AsBytes()));

        UpdateTemplate("with-package-files.txt");

        await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

        FileAssert.Exists(Path.Combine(_to.Location, GenerateCommand.OutputFileName));

        var output = File.ReadAllText(Path.Combine(_to.Location, GenerateCommand.OutputFileName));
        output.ShouldContain(package.Name);
        output.ShouldContain(package.Version);
        output.ShouldContain(package.HRef);
        output.ShouldContain(package.Author);
        output.ShouldContain(package.Copyright);
        output.ShouldContain(package.ThirdPartyNotices);

        output.ShouldContain(package.Licenses[0].HRef);
        output.ShouldContain("newtonsoft.json.md");
        output.ShouldContain(license.FullName);

        FileAssert.Exists(Path.Combine(_to.Location, "Licenses", "newtonsoft.json.md"));

        output = File.ReadAllText(Path.Combine(_to.Location, "Licenses", "newtonsoft.json.md"));
        output.ShouldBe("newtonsoft license text");
    }

    [Test]
    public async Task SkipPackage()
    {
        var package1 = new Package
        {
            SourceCode = PackageSources.NuGet,
            Name = "internal package",
            Version = "1",
            UsedBy = new[] { PackageApplication.Internal(AppName) }
        };
        _packages.Add(package1);

        var package2 = new Package
        {
            SourceCode = PackageSources.NuGet,
            Name = "other application",
            Version = "1",
            UsedBy = new[] { PackageApplication.Public(AppName + "2") }
        };
        _packages.Add(package2);

        await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

        FileAssert.Exists(Path.Combine(_to.Location, GenerateCommand.OutputFileName));

        var output = File.ReadAllText(Path.Combine(_to.Location, GenerateCommand.OutputFileName));
        output.ShouldNotContain(package1.Name);
        output.ShouldNotContain(package2.Name);
    }

    private void UpdateTemplate(string fileName)
    {
        var destFileName = Path.Combine(_to.Location, "configuration", StorageExtensions.ThirdPartyNoticesTemplateFileName);
        using (var source = TempFile.OpenResource(GetType(), "GenerateCommandTemplates." + fileName))
        using (var dest = new FileStream(destFileName, FileMode.Create, FileAccess.ReadWrite))
        {
            source.CopyTo(dest);
        }
    }
}