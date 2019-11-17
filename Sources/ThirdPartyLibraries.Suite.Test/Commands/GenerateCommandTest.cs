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
using Unity;

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
        private IList<PackageNotices> _noticeses;

        [SetUp]
        public void BeforeEachTest()
        {
            _logger = new Mock<ILogger>(MockBehavior.Strict);
            
            _to = new TempFolder();
            Directory.CreateDirectory(Path.Combine(_to.Location, "configuration"));

            var container = new UnityContainer();
            _noticeses = new List<PackageNotices>();

            _storage = new Mock<IStorage>(MockBehavior.Strict);
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
                .ReturnsAsync(() => _noticeses.Select(i => new LibraryId(PackageSources.NuGet, i.Name, i.Version)).ToArray());

            _packageRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
            _packageRepository
                .SetupGet(r => r.Storage)
                .Returns(_storage.Object);
            _packageRepository
                .Setup(r => r.LoadPackagesNoticesAsync(It.IsAny<LibraryId>(), CancellationToken.None))
                .Returns<LibraryId, CancellationToken>((id, _) =>
                {
                    var package = _noticeses.Single(i => i.Name == id.Name && i.Version == id.Version);
                    return Task.FromResult(package);
                });
            container.RegisterInstance(_packageRepository.Object);

            _sut = new GenerateCommand(container, _logger.Object)
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
            var notice = new PackageNotices
            {
                Name = "Newtonsoft.Json",
                Version = "12.0.2",
                LicenseCode = "MIT",
                HRef = "https://www.nuget.org/packages/Newtonsoft.Json/12.0.2",
                Author = "James Newton-King",
                Copyright = "Copyright © James Newton-King 2008",
                UsedBy = new[] { new PackageNoticesApplication(AppName, false) },
                ThirdPartyNotices = "some extra notices"
            };
            _noticeses.Add(notice);

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

            await _sut.ExecuteAsync(CancellationToken.None);

            FileAssert.Exists(Path.Combine(_to.Location, GenerateCommand.OutputFileName));
            FileAssert.Exists(Path.Combine(_to.Location, "Licenses", "MIT-license.txt"));

            var output = File.ReadAllText(Path.Combine(_to.Location, GenerateCommand.OutputFileName));
            output.ShouldContain(notice.Name);
            output.ShouldContain(notice.Version);
            output.ShouldContain(notice.HRef);
            output.ShouldContain(notice.Author);
            output.ShouldContain(notice.Copyright);
            output.ShouldContain(notice.ThirdPartyNotices);

            output.ShouldContain(license.HRef);
            output.ShouldContain(license.FileName);
            output.ShouldContain(license.FullName);

            output = File.ReadAllText(Path.Combine(_to.Location, "Licenses", "MIT-license.txt"));
            output.ShouldBe("MIT license text");
        }

        [Test]
        public async Task SkipPackage()
        {
            var notice1 = new PackageNotices
            {
                Name = "internal package",
                Version = "1",
                UsedBy = new[] { new PackageNoticesApplication(AppName, true) }
            };
            _noticeses.Add(notice1);

            var notice2 = new PackageNotices
            {
                Name = "other application",
                Version = "1",
                UsedBy = new[] { new PackageNoticesApplication(AppName + "2", false) }
            };
            _noticeses.Add(notice2);

            await _sut.ExecuteAsync(CancellationToken.None);

            FileAssert.Exists(Path.Combine(_to.Location, GenerateCommand.OutputFileName));

            var output = File.ReadAllText(Path.Combine(_to.Location, GenerateCommand.OutputFileName));
            output.ShouldNotContain(notice1.Name);
            output.ShouldNotContain(notice2.Name);
        }
    }
}
