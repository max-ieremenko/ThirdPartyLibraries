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

namespace ThirdPartyLibraries.Suite.Commands
{
    [TestFixture]
    public class ValidateCommandTest
    {
        private const string AppName = "App";

        private ValidateCommand _sut;
        private Mock<IPackageRepository> _packageRepository;
        private Mock<IStorage> _storage;
        private IList<PackageInfo> _storagePackages;
        private IList<LibraryReference> _sourceReferences;
        private LicenseIndexJson _mitLicense;
        private IList<string> _loggerErrors;
        private IServiceProvider _serviceProvider;

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

            _storagePackages = new List<PackageInfo>();
            _sourceReferences = new List<LibraryReference>();

            _mitLicense = new LicenseIndexJson
            {
                Code = "MIT",
                RequiresApproval = false,
                RequiresThirdPartyNotices = false
            };

            _storage = new Mock<IStorage>(MockBehavior.Strict);
            _storage
                .Setup(s => s.GetAllLibrariesAsync(CancellationToken.None))
                .ReturnsAsync(() => _storagePackages.Select(i => i.Id).ToArray());
            _storage
                .Setup(s => s.OpenLicenseFileReadAsync(_mitLicense.Code, "index.json", CancellationToken.None))
                .ReturnsAsync(_mitLicense.JsonSerialize);

            _packageRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
            _packageRepository
                .SetupGet(r => r.Storage)
                .Returns(_storage.Object);
            _packageRepository
                .Setup(r => r.LoadPackageAsync(It.IsAny<LibraryId>(), CancellationToken.None))
                .Returns<LibraryId, CancellationToken>((id, _) =>
                {
                    var package = _storagePackages.Single(i => i.Package.Name == id.Name && i.Package.SourceCode == id.SourceCode && i.Package.Version == id.Version);
                    return Task.FromResult(package.Package);
                });
            _packageRepository
                .Setup(r => r.LoadPackageAsync(It.IsAny<LibraryId>(), CancellationToken.None))
                .Returns<LibraryId, CancellationToken>((id, _) =>
                {
                    var package = _storagePackages.Single(i => i.Package.Name == id.Name && i.Package.SourceCode == id.SourceCode && i.Package.Version == id.Version);
                    return Task.FromResult(package.Package);
                });

            var parser = new Mock<ISourceCodeParser>(MockBehavior.Strict);
            parser
                .Setup(p => p.GetReferences(It.IsNotNull<IList<string>>()))
                .Returns<IList<string>>(locations =>
                {
                    locations.ShouldBe(new[] { "source1", "source2" });
                    return _sourceReferences;
                });

            var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            serviceProvider
                .Setup(p => p.GetService(typeof(IPackageRepository)))
                .Returns(_packageRepository.Object);
            serviceProvider
                .Setup(p => p.GetService(typeof(ISourceCodeParser)))
                .Returns(parser.Object);
            serviceProvider
                .Setup(p => p.GetService(typeof(ILogger)))
                .Returns(logger.Object);
            _serviceProvider = serviceProvider.Object;

            _sut = new ValidateCommand
            {
                Sources = { "source1", "source2" },
                AppName = AppName
            };
        }

        [Test]
        public async Task Success()
        {
            var package = new PackageInfo
            {
                Package = new Package
                {
                    SourceCode = PackageSources.NuGet,
                    Name = "Newtonsoft.Json",
                    Version = "12.0.2",
                    ApprovalStatus = PackageApprovalStatus.Approved,
                    LicenseCode = _mitLicense.Code,
                    UsedBy = new[] { new PackageApplication(AppName, false) }
                }
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                package.Id,
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            var actual = await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

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

            var actual = await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("not found in the repository"));
        }

        [Test]
        public async Task PackageIsNotAssignedToApp()
        {
            var package = new PackageInfo
            {
                Package = new Package
                {
                    SourceCode = "package-source",
                    Name = "package-name",
                    Version = "package-version",
                    ApprovalStatus = PackageApprovalStatus.Approved,
                    LicenseCode = _mitLicense.Code
                }
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                package.Id,
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            var actual = await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("are not assigned to " + AppName));
        }

        [Test]
        public async Task PackageHasNoLicense()
        {
            var package = new PackageInfo
            {
                Package = new Package
                {
                    SourceCode = "package-source",
                    Name = "package-name",
                    Version = "package-version",
                    ApprovalStatus = PackageApprovalStatus.Approved,
                    UsedBy = new[] { new PackageApplication(AppName, false) }
                }
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                package.Id,
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            var actual = await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("have no license"));
        }

        [Test]
        public async Task PackageIsNotApproved()
        {
            var package = new PackageInfo
            {
                Package = new Package
                {
                    SourceCode = "package-source",
                    Name = "package-name",
                    Version = "package-version",
                    LicenseCode = _mitLicense.Code,
                    ApprovalStatus = PackageApprovalStatus.HasToBeApproved,
                    UsedBy = new[] { new PackageApplication(AppName, false) }
                }
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                package.Id,
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            var actual = await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("are not approved"));
        }

        [Test]
        public async Task PackageIsTrash()
        {
            var package = new PackageInfo
            {
                Package = new Package
                {
                    SourceCode = "package-source",
                    Name = "package-name",
                    Version = "package-version",
                    LicenseCode = _mitLicense.Code,
                    ApprovalStatus = PackageApprovalStatus.AutomaticallyApproved,
                    UsedBy = new[] { new PackageApplication(AppName, false) }
                }
            };
            _storagePackages.Add(package);

            var actual = await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("but references not found in the sources"));
        }

        [Test]
        public async Task PackageAutomaticallyApprovedIsNotApproved()
        {
            var package = new PackageInfo
            {
                Package = new Package
                {
                    SourceCode = "package-source",
                    Name = "package-name",
                    Version = "package-version",
                    LicenseCode = _mitLicense.Code,
                    ApprovalStatus = PackageApprovalStatus.AutomaticallyApproved,
                    UsedBy = new[] { new PackageApplication(AppName, false) }
                }
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                package.Id,
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            _mitLicense.RequiresApproval = true;

            var actual = await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("are not approved"));
        }

        [Test]
        public async Task PackageHasNoThirdPartyNotices()
        {
            var package = new PackageInfo
            {
                Package = new Package
                {
                    SourceCode = "package-source",
                    Name = "package-name",
                    Version = "package-version",
                    LicenseCode = _mitLicense.Code,
                    ApprovalStatus = PackageApprovalStatus.AutomaticallyApproved,
                    UsedBy = new[] { new PackageApplication(AppName, false) }
                }
            };
            _storagePackages.Add(package);

            _sourceReferences.Add(new LibraryReference(
                package.Id,
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            _mitLicense.RequiresThirdPartyNotices = true;

            var actual = await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

            actual.ShouldBeFalse();
            _loggerErrors.ShouldContain(i => i.Contains("have no third party notices"));
        }

        private sealed class PackageInfo
        {
            public LibraryId Id => new LibraryId(Package.SourceCode, Package.Name, Package.Version);

            public Package Package { get; set; }
        }
    }
}
