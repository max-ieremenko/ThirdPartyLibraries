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
        private IServiceProvider _serviceProvider;

        [SetUp]
        public void BeforeEachTest()
        {
            var logger = new Mock<ILogger>(MockBehavior.Strict);
            logger
                .Setup(l => l.Indent())
                .Returns((IDisposable)null);

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

            await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public void ReferenceNotFoundInRepository()
        {
            _sourceReferences.Add(new LibraryReference(
                new LibraryId("package-source", "package-name", "package-version"),
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                false));

            var ex = Assert.ThrowsAsync<RepositoryValidationException>(() => _sut.ExecuteAsync(_serviceProvider, CancellationToken.None));

            ex.Errors.Length.ShouldBe(1);
            ex.Errors[0].Issue.ShouldContain("not found in the repository");

            ex.Errors[0].Libraries.Length.ShouldBe(1);
            ex.Errors[0].Libraries[0].ShouldBe(new LibraryId("package-source", "package-name", "package-version"));
        }

        [Test]
        public void PackageIsNotAssignedToApp()
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

            var ex = Assert.ThrowsAsync<RepositoryValidationException>(() => _sut.ExecuteAsync(_serviceProvider, CancellationToken.None));

            ex.Errors.Length.ShouldBe(1);
            ex.Errors[0].Issue.ShouldContain("are not assigned to " + AppName);

            ex.Errors[0].Libraries.Length.ShouldBe(1);
            ex.Errors[0].Libraries[0].ShouldBe(new LibraryId("package-source", "package-name", "package-version"));
        }

        [Test]
        public void PackageHasNoLicense()
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

            var ex = Assert.ThrowsAsync<RepositoryValidationException>(() => _sut.ExecuteAsync(_serviceProvider, CancellationToken.None));

            ex.Errors.Length.ShouldBe(1);
            ex.Errors[0].Issue.ShouldContain("have no license");

            ex.Errors[0].Libraries.Length.ShouldBe(1);
            ex.Errors[0].Libraries[0].ShouldBe(new LibraryId("package-source", "package-name", "package-version"));
        }

        [Test]
        public void PackageIsNotApproved()
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

            var ex = Assert.ThrowsAsync<RepositoryValidationException>(() => _sut.ExecuteAsync(_serviceProvider, CancellationToken.None));

            ex.Errors.Length.ShouldBe(1);
            ex.Errors[0].Issue.ShouldContain("are not approved");

            ex.Errors[0].Libraries.Length.ShouldBe(1);
            ex.Errors[0].Libraries[0].ShouldBe(new LibraryId("package-source", "package-name", "package-version"));
        }

        [Test]
        public void PackageIsTrash()
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

            var ex = Assert.ThrowsAsync<RepositoryValidationException>(() => _sut.ExecuteAsync(_serviceProvider, CancellationToken.None));

            ex.Errors.Length.ShouldBe(1);
            ex.Errors[0].Issue.ShouldContain("but references not found in the sources");

            ex.Errors[0].Libraries.Length.ShouldBe(1);
            ex.Errors[0].Libraries[0].ShouldBe(new LibraryId("package-source", "package-name", "package-version"));
        }

        [Test]
        public void PackageAutomaticallyApprovedIsNotApproved()
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

            var ex = Assert.ThrowsAsync<RepositoryValidationException>(() => _sut.ExecuteAsync(_serviceProvider, CancellationToken.None));

            ex.Errors.Length.ShouldBe(1);
            ex.Errors[0].Issue.ShouldContain("are not approved");

            ex.Errors[0].Libraries.Length.ShouldBe(1);
            ex.Errors[0].Libraries[0].ShouldBe(new LibraryId("package-source", "package-name", "package-version"));
        }

        [Test]
        public void PackageHasNoThirdPartyNotices()
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

            var ex = Assert.ThrowsAsync<RepositoryValidationException>(() => _sut.ExecuteAsync(_serviceProvider, CancellationToken.None));

            ex.Errors.Length.ShouldBe(1);
            ex.Errors[0].Issue.ShouldContain("have no third party notices");

            ex.Errors[0].Libraries.Length.ShouldBe(1);
            ex.Errors[0].Libraries[0].ShouldBe(new LibraryId("package-source", "package-name", "package-version"));
        }

        private sealed class PackageInfo
        {
            public LibraryId Id => new LibraryId(Package.SourceCode, Package.Name, Package.Version);

            public Package Package { get; set; }
        }
    }
}
