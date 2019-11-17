using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal
{
    [TestFixture]
    public class PackageRepositoryTest
    {
        private IUnityContainer _container;
        private Mock<IStorage> _storage;
        private Mock<ILicenseResolver> _licenseResolver;
        private PackageRepository _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            _container = new UnityContainer();
            _storage = new Mock<IStorage>(MockBehavior.Strict);

            _licenseResolver = new Mock<ILicenseResolver>(MockBehavior.Strict);
            _container.RegisterInstance(_licenseResolver.Object);

            _sut = new PackageRepository(_container, _storage.Object);
        }

        [Test]
        public void ResolveAdapter()
        {
            _container.RegisterType<IPackageRepositoryAdapter, PackageRepositoryAdapter>("code");

            var actual = _sut.ResolveAdapter("code");

            actual.ShouldBeOfType<PackageRepositoryAdapter>();
            actual.Storage.ShouldBe(_storage.Object);
        }

        [Test]
        public async Task LoadLicenseFromRepository()
        {
            _storage
                .Setup(r => r.OpenLicenseFileReadAsync("MIT", "index.json", CancellationToken.None))
                .ReturnsAsync(new LicenseIndexJson
                {
                    RequiresApproval = true,
                    Code = "MIT",
                    HRef = "some link"
                }.JsonSerialize());

            var actual = await _sut.LoadOrCreateLicenseAsync("MIT", CancellationToken.None);

            actual.ShouldNotBeNull();
            actual.Code.ShouldBe("MIT");
            actual.RequiresApproval.ShouldBeTrue();
        }

        [Test]
        public async Task LoadLicenseCreateNewResolved()
        {
            _storage
                .Setup(r => r.OpenLicenseFileReadAsync("MIT", "index.json", CancellationToken.None))
                .ReturnsAsync((Stream)null);

            _licenseResolver
                .Setup(l => l.DownloadByCodeAsync("MIT", CancellationToken.None))
                .ReturnsAsync(new LicenseInfo
                {
                    Code = "MIT",
                    FullName = "MIT license",
                    FileName = "fileName.txt",
                    FileContent = "text".AsBytes(),
                    FileHRef = "some link"
                });

            _storage
                .Setup(r => r.CreateLicenseFileAsync("MIT", "index.json", It.IsNotNull<byte[]>(), CancellationToken.None))
                .Callback<string, string, byte[], CancellationToken>((a, b, content, _) =>
                {
                    var model = content.JsonDeserialize<LicenseIndexJson>();
                    model.Code.ShouldBe("MIT");
                    model.FullName.ShouldBe("MIT license");
                    model.FileName.ShouldBe("fileName.txt");
                    model.HRef.ShouldBe("some link");
                    model.RequiresApproval.ShouldBeTrue();
                })
                .Returns(Task.CompletedTask);

            _storage
                .Setup(r => r.CreateLicenseFileAsync("MIT", "fileName.txt", It.Is<byte[]>(i => i.AsText() == "text"), CancellationToken.None))
                .Returns(Task.CompletedTask);

            var actual = await _sut.LoadOrCreateLicenseAsync("MIT", CancellationToken.None);

            _storage.VerifyAll();

            actual.ShouldNotBeNull();
            actual.Code.ShouldBe("MIT");
            actual.RequiresApproval.ShouldBeTrue();
        }

        [Test]
        public async Task LoadLicenseCreateNewDefault()
        {
            _storage
                .Setup(r => r.OpenLicenseFileReadAsync("MIT", "index.json", CancellationToken.None))
                .ReturnsAsync((Stream)null);

            _licenseResolver
                .Setup(l => l.DownloadByCodeAsync("MIT", CancellationToken.None))
                .ReturnsAsync((LicenseInfo)null);

            _storage
                .Setup(r => r.CreateLicenseFileAsync("MIT", "index.json", It.IsNotNull<byte[]>(), CancellationToken.None))
                .Callback<string, string, byte[], CancellationToken>((a, b, content, _) =>
                {
                    var model = content.JsonDeserialize<LicenseIndexJson>();
                    model.Code.ShouldBe("MIT");
                    model.FileName.ShouldBe("license.txt");
                    model.HRef.ShouldBeNull();
                    model.RequiresApproval.ShouldBeTrue();
                })
                .Returns(Task.CompletedTask);

            _storage
                .Setup(r => r.CreateLicenseFileAsync("MIT", "license.txt", Array.Empty<byte>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            var actual = await _sut.LoadOrCreateLicenseAsync("MIT", CancellationToken.None);

            _storage.VerifyAll();

            actual.ShouldNotBeNull();
            actual.Code.ShouldBe("MIT");
            actual.RequiresApproval.ShouldBeTrue();
        }

        [Test]
        public async Task UpdateAllPackagesReadMe()
        {
            var libraryId = new LibraryId("source", "name", "version");
            var metadata = new PackageReadMe();

            _storage
                .Setup(r => r.GetAllLibrariesAsync(CancellationToken.None))
                .ReturnsAsync(new[] { libraryId });

            var adapter = new Mock<IPackageRepositoryAdapter>(MockBehavior.Strict);
            _container.RegisterInstance("source", adapter.Object);

            adapter
                .Setup(a => a.UpdatePackageReadMeAsync(libraryId, CancellationToken.None))
                .ReturnsAsync(metadata);

            var actual = await _sut.UpdateAllPackagesReadMeAsync(CancellationToken.None);

            actual.Count.ShouldBe(1);
            actual[0].ShouldBe(metadata);

            _storage.VerifyAll();
            adapter.VerifyAll();
        }

        [Test]
        public async Task RemoveFromApplicationKeepPackage()
        {
            var libraryId = new LibraryId("source", "name", "version");

            var adapter = new Mock<IPackageRepositoryAdapter>(MockBehavior.Strict);
            adapter
                .Setup(a => a.RemoveFromApplicationAsync(libraryId, "app1", CancellationToken.None))
                .ReturnsAsync(PackageRemoveResult.Removed);

            _container.RegisterInstance(libraryId.SourceCode, adapter.Object);

            var actual = await _sut.RemoveFromApplicationAsync(libraryId, "app1", CancellationToken.None);

            actual.ShouldBe(PackageRemoveResult.Removed);
        }

        [Test]
        public async Task RemoveFromApplicationRemovePackage()
        {
            var libraryId = new LibraryId("source", "name", "version");

            var adapter = new Mock<IPackageRepositoryAdapter>(MockBehavior.Strict);
            adapter
                .Setup(a => a.RemoveFromApplicationAsync(libraryId, "app1", CancellationToken.None))
                .ReturnsAsync(PackageRemoveResult.RemovedNoRefs);

            _storage
                .Setup(r => r.RemoveLibraryAsync(libraryId, CancellationToken.None))
                .Returns(Task.CompletedTask);

            _container.RegisterInstance(libraryId.SourceCode, adapter.Object);

            var actual = await _sut.RemoveFromApplicationAsync(libraryId, "app1", CancellationToken.None);

            actual.ShouldBe(PackageRemoveResult.RemovedNoRefs);
            _storage.VerifyAll();
        }

        private sealed class PackageRepositoryAdapter : IPackageRepositoryAdapter
        {
            [Dependency]
            public IStorage Storage { get; set; }

            public Task<Package> LoadPackageAsync(LibraryId id, CancellationToken token) => throw new NotImplementedException();

            public Task UpdatePackageAsync(LibraryReference reference, Package package, string appName, CancellationToken token) => throw new NotImplementedException();

            public Task<PackageReadMe> UpdatePackageReadMeAsync(LibraryId id, CancellationToken none) => throw new NotImplementedException();

            public Task<PackageNotices> LoadPackageNoticesAsync(LibraryId id, CancellationToken token) => throw new NotImplementedException();

            public ValueTask<PackageRemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token) => throw new NotImplementedException();
        }
    }
}
