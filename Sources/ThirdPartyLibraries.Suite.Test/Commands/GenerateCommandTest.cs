using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands
{
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

            var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            serviceProvider
                .Setup(p => p.GetService(typeof(IPackageRepository)))
                .Returns(_packageRepository.Object);
            serviceProvider
                .Setup(p => p.GetService(typeof(ILogger)))
                .Returns(_logger.Object);
            _serviceProvider = serviceProvider.Object;

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
        public async Task GenerateThirdPartyNotices()
        {
            var package = new Package
            {
                Name = "Newtonsoft.Json",
                Version = "12.0.2",
                LicenseCode = "MIT",
                HRef = "https://www.nuget.org/packages/Newtonsoft.Json/12.0.2",
                Author = "James Newton-King",
                Copyright = "Copyright © James Newton-King 2008",
                UsedBy = new[] { new PackageApplication(AppName, false) },
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
        public async Task SkipPackage()
        {
            var package1 = new Package
            {
                Name = "internal package",
                Version = "1",
                UsedBy = new[] { new PackageApplication(AppName, true) }
            };
            _packages.Add(package1);

            var package2 = new Package
            {
                Name = "other application",
                Version = "1",
                UsedBy = new[] { new PackageApplication(AppName + "2", false) }
            };
            _packages.Add(package2);

            await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

            FileAssert.Exists(Path.Combine(_to.Location, GenerateCommand.OutputFileName));

            var output = File.ReadAllText(Path.Combine(_to.Location, GenerateCommand.OutputFileName));
            output.ShouldNotContain(package1.Name);
            output.ShouldNotContain(package2.Name);
        }

        [Test]
        public async Task CopyDependentLicense()
        {
            var package = new Package
            {
                Name = "name",
                Version = "version",
                LicenseCode = "LGPL-3.0",
                UsedBy = new[] { new PackageApplication(AppName, false) }
            };
            _packages.Add(package);

            var lgplLicense = new LicenseIndexJson
            {
                Code = "LGPL-3.0",
                HRef = "https://spdx.org/licenses/LGPL-3.0",
                FullName = "GNU Lesser General Public License v3.0"
            };

            var gplLicense = new LicenseIndexJson
            {
                Code = "GPL-3.0",
                HRef = "https://spdx.org/licenses/GPL-3.0",
                FileName = "license.txt",
                FullName = "GNU General Public License v3.0"
            };

            var mitLicense = new LicenseIndexJson
            {
                Code = "MIT",
                HRef = "https://spdx.org/licenses/MIT.html",
                FileName = "license.txt",
                FullName = "MIT License"
            };

            lgplLicense.Dependencies = new[] { gplLicense.Code };
            gplLicense.Dependencies = new[] { mitLicense.Code };
            mitLicense.Dependencies = new[] { lgplLicense.Code };

            _storage
                .Setup(s => s.OpenLicenseFileReadAsync(lgplLicense.Code, "index.json", CancellationToken.None))
                .ReturnsAsync(lgplLicense.JsonSerialize);

            _storage
                .Setup(s => s.OpenLicenseFileReadAsync(gplLicense.Code, "index.json", CancellationToken.None))
                .ReturnsAsync(gplLicense.JsonSerialize);

            _storage
                .Setup(s => s.OpenLicenseFileReadAsync(mitLicense.Code, "index.json", CancellationToken.None))
                .ReturnsAsync(mitLicense.JsonSerialize);
            
            _storage
                            .Setup(s => s.OpenLicenseFileReadAsync(gplLicense.Code, "license.txt", CancellationToken.None))
                .ReturnsAsync(() => new MemoryStream("GPL-3.0 license text".AsBytes()));

            _storage
                .Setup(s => s.OpenLicenseFileReadAsync(mitLicense.Code, "license.txt", CancellationToken.None))
                .ReturnsAsync(() => new MemoryStream("MIT license text".AsBytes()));

            await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

            FileAssert.Exists(Path.Combine(_to.Location, "Licenses", "MIT-license.txt"));
            FileAssert.Exists(Path.Combine(_to.Location, "Licenses", "GPL-3.0-license.txt"));

            var output = File.ReadAllText(Path.Combine(_to.Location, "Licenses", "MIT-license.txt"));
            output.ShouldBe("MIT license text");
            
            output = File.ReadAllText(Path.Combine(_to.Location, "Licenses", "GPL-3.0-license.txt"));
            output.ShouldBe("GPL-3.0 license text");
        }
    }
}
