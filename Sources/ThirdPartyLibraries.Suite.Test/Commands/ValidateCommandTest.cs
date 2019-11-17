using System;
using System.Collections.Generic;
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
    public class ValidateCommandTest
    {
        private const string AppName = "App";

        private ValidateCommand _sut;
        private Mock<IPackageRepository> _packageRepository;
        private Mock<IStorage> _storage;
        private IList<Package> _storagePackages;
        private IList<LibraryReference> _sourceReferences;
        private IList<string> _loggerErrors;

        [SetUp]
        public void BeforeEachTest()
        {
            _loggerErrors = new List<string>();

            var logger = new Mock<ILogger>(MockBehavior.Strict);
            logger
                .Setup(l => l.Indent())
                .Returns((IDisposable)null);
            logger
                .Setup(l => l.Error(It.IsNotNull<string>()))
                .Callback<string>(message =>
                {
                    Console.WriteLine("Error: " + message);
                    _loggerErrors.Add(message);
                });

            _storagePackages = new List<Package>();
            _sourceReferences = new List<LibraryReference>();

            var container = new UnityContainer();

            _storage = new Mock<IStorage>(MockBehavior.Strict);
            _storage
                .Setup(s => s.GetAllLibrariesAsync(CancellationToken.None))
                .ReturnsAsync(() => _storagePackages.Select(i => new LibraryId(i.SourceCode, i.Name, i.Version)).ToArray());

            _packageRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
            _packageRepository
                .SetupGet(r => r.Storage)
                .Returns(_storage.Object);
            _packageRepository
                .Setup(r => r.LoadPackageAsync(It.IsAny<LibraryId>(), CancellationToken.None))
                .Returns<LibraryId, CancellationToken>((id, _) =>
                {
                    var package = _storagePackages.Single(i => i.Name == id.Name && i.SourceCode == id.SourceCode && i.Version == id.Version);
                    return Task.FromResult(package);
                });
            container.RegisterInstance(_packageRepository.Object);

            var parser = new Mock<ISourceCodeParser>(MockBehavior.Strict);
            parser
                .Setup(p => p.GetReferences(It.IsNotNull<IList<string>>()))
                .Returns<IList<string>>(locations =>
                {
                    locations.ShouldBe(new[] { "source1", "source2" });
                    return _sourceReferences;
                });
            container.RegisterInstance(parser.Object);

            _sut = new ValidateCommand(container, logger.Object)
            {
                Sources = { "source1", "source2" },
                AppName = AppName
            };
        }

        [Test]
        public async Task Success()
        {
            var package = new Package
            {
                SourceCode = PackageSources.NuGet,
                Name = "Newtonsoft.Json",
                Version = "12.0.2",
                ApprovalStatus = PackageApprovalStatus.Approved,
                LicenseCode = "MIT",
                UsedBy = new[] { AppName }
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                new LibraryId(package.SourceCode, package.Name, package.Version),
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            var actual = await _sut.ExecuteAsync(CancellationToken.None);

            actual.ShouldBeTrue();
            _loggerErrors.ShouldBeEmpty();
        }

        [Test]
        public async Task ReferenceNotFoundInRepository()
        {
            _sourceReferences.Add(new LibraryReference(
                new LibraryId("package-source", "package-name", "package-version"),
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            var actual = await _sut.ExecuteAsync(CancellationToken.None);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("not found in the repository"));
        }

        [Test]
        public async Task PackageIsNotAssignedToApp()
        {
            var package = new Package
            {
                SourceCode = "package-source",
                Name = "package-name",
                Version = "package-version",
                ApprovalStatus = PackageApprovalStatus.Approved,
                LicenseCode = "MIT"
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                new LibraryId(package.SourceCode, package.Name, package.Version),
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            var actual = await _sut.ExecuteAsync(CancellationToken.None);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("are not assigned to " + AppName));
        }

        [Test]
        public async Task PackageHasNoLicense()
        {
            var package = new Package
            {
                SourceCode = "package-source",
                Name = "package-name",
                Version = "package-version",
                ApprovalStatus = PackageApprovalStatus.Approved,
                UsedBy = new[] { AppName }
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                new LibraryId(package.SourceCode, package.Name, package.Version),
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            var actual = await _sut.ExecuteAsync(CancellationToken.None);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("have no license"));
        }

        [Test]
        public async Task PackageIsNotApproved()
        {
            var package = new Package
            {
                SourceCode = "package-source",
                Name = "package-name",
                Version = "package-version",
                LicenseCode = "MIT",
                ApprovalStatus = PackageApprovalStatus.HasToBeApproved,
                UsedBy = new[] { AppName }
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                new LibraryId(package.SourceCode, package.Name, package.Version),
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            var actual = await _sut.ExecuteAsync(CancellationToken.None);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("are not approved"));
        }

        [Test]
        public async Task PackageAutomaticallyApprovedIsNotApproved()
        {
            var package = new Package
            {
                SourceCode = "package-source",
                Name = "package-name",
                Version = "package-version",
                LicenseCode = "MIT",
                ApprovalStatus = PackageApprovalStatus.AutomaticallyApproved,
                UsedBy = new[] { AppName }
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                new LibraryId(package.SourceCode, package.Name, package.Version),
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            _storage
                .Setup(s => s.OpenLicenseFileReadAsync("MIT", "index.json", CancellationToken.None))
                .ReturnsAsync(new LicenseIndexJson { RequiresApproval = true }.JsonSerialize);

            var actual = await _sut.ExecuteAsync(CancellationToken.None);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("are not approved"));
        }
    }
}
