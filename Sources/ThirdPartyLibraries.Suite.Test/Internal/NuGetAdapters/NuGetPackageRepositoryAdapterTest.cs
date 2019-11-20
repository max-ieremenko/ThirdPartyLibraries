using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    [TestFixture]
    public class NuGetPackageRepositoryAdapterTest
    {
        private const string AppName = "some app";

        private Mock<IStorage> _repository;
        private Mock<INuGetApi> _api;
        private NuGetPackageRepositoryAdapter _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            _repository = new Mock<IStorage>(MockBehavior.Strict);
            _api = new Mock<INuGetApi>(MockBehavior.Strict);

            _sut = new NuGetPackageRepositoryAdapter
            {
                Storage = _repository.Object,
                Api = _api.Object,
                Configuration = new NuGetConfiguration()
            };
        }

        [Test]
        public async Task LoadPackage()
        {
            var libraryId = new LibraryId(PackageSources.NuGet, "Newtonsoft.Json", "12.0.2");
            var specContent = new MemoryStream();

            _repository
                .Setup(r => r.OpenLibraryFileReadAsync(libraryId, NuGetConstants.RepositorySpecFileName, CancellationToken.None))
                .ReturnsAsync(specContent);

            _repository
                .Setup(r => r.OpenLibraryFileReadAsync(libraryId, "index.json", CancellationToken.None))
                .ReturnsAsync(new NuGetLibraryIndexJson
                {
                    License =
                    {
                        Code = "MIT",
                        Status = PackageApprovalStatus.Approved.ToString()
                    }
                }.JsonSerialize());

            _api
                .Setup(a => a.ParseSpec(specContent))
                .Returns(new NuGetSpec
                {
                    Id = libraryId.Name,
                    Version = libraryId.Version
                });

            var actual = await _sut.LoadPackageAsync(libraryId, CancellationToken.None);

            actual.ShouldNotBeNull();
            actual.Name.ShouldBe(libraryId.Name);
            actual.Version.ShouldBe(libraryId.Version);
            actual.LicenseCode.ShouldBe("MIT");
            actual.ApprovalStatus.ShouldBe(PackageApprovalStatus.Approved);
        }

        [Test]
        public async Task LoadPackageNotFound()
        {
            var libraryId = new LibraryId(PackageSources.NuGet, "Newtonsoft.Json", "12.0.2");

            _repository
                .Setup(r => r.OpenLibraryFileReadAsync(libraryId, NuGetConstants.RepositorySpecFileName, CancellationToken.None))
                .ReturnsAsync((Stream)null);

            _repository
                .Setup(r => r.OpenLibraryFileReadAsync(libraryId, "index.json", CancellationToken.None))
                .ReturnsAsync((Stream)null);

            var actual = await _sut.LoadPackageAsync(libraryId, CancellationToken.None);

            actual.ShouldBeNull();
        }

        [Test]
        public async Task InsertPackage()
        {
            var libraryId = new LibraryId(PackageSources.NuGet, "Newtonsoft.Json", "12.0.2");
            var reference = new LibraryReference(
                libraryId,
                new[] { "net472", "netcoreapp3.0" },
                new[] { new LibraryId(PackageSources.NuGet, "Microsoft.CSharp", "4.3.0") },
                true);
            var package = new Package
            {
                SourceCode = reference.Id.SourceCode,
                Name = reference.Id.Name,
                Version = reference.Id.Version,
                ApprovalStatus = PackageApprovalStatus.AutomaticallyApproved,
                LicenseCode = "MIT",
                Licenses =
                {
                    new PackageLicense
                    {
                        Code = "MIT",
                        CodeDescription = "description",
                        Subject = NuGetPackageResolver.LicenseSubjectPackage,
                        HRef = "the link"
                    }
                },
                Attachments =
                {
                    new PackageAttachment(NuGetConstants.RepositorySpecFileName, "spec".AsBytes()),
                    new PackageAttachment("package-LICENSE.md", "package".AsBytes()),
                    new PackageAttachment("repository-LICENSE.md", "repository".AsBytes())
                }
            };

            _repository
                .Setup(r => r.OpenLibraryFileReadAsync(libraryId, "index.json", CancellationToken.None))
                .ReturnsAsync((Stream)null);

            foreach (var attachment in package.Attachments)
            {
                _repository
                    .Setup(r => r.WriteLibraryFileAsync(libraryId, attachment.Name, attachment.Content, CancellationToken.None))
                    .Returns(Task.CompletedTask);
            }

            _repository
                .Setup(r => r.WriteLibraryFileAsync(libraryId, "index.json", It.IsNotNull<byte[]>(), CancellationToken.None))
                .Callback<LibraryId, string, byte[], CancellationToken>((id, fileName, content, __) =>
                {
                    var model = content.JsonDeserialize<NuGetLibraryIndexJson>();

                    model.UsedBy.Count.ShouldBe(1);
                    model.UsedBy[0].Name.ShouldBe(AppName);
                    model.UsedBy[0].InternalOnly.ShouldBeTrue();
                    model.UsedBy[0].TargetFrameworks.ShouldBe(reference.TargetFrameworks);
                    model.UsedBy[0].Dependencies.Count.ShouldBe(1);

                    model.UsedBy[0].Dependencies[0].Name.ShouldBe("Microsoft.CSharp");
                    model.UsedBy[0].Dependencies[0].Version.ShouldBe("4.3.0");

                    model.License.Code.ShouldBe("MIT");
                    model.License.Status.ShouldBe(PackageApprovalStatus.AutomaticallyApproved.ToString());

                    model.Licenses.Count.ShouldBe(1);
                    model.Licenses[0].Subject.ShouldBe(NuGetPackageResolver.LicenseSubjectPackage);
                    model.Licenses[0].Code.ShouldBe("MIT");
                    model.Licenses[0].Description.ShouldBe("description");
                    model.Licenses[0].HRef.ShouldBe("the link");
                })
                .Returns(Task.CompletedTask);

            await _sut.UpdatePackageAsync(reference, package, AppName, CancellationToken.None);

            _repository.VerifyAll();
        }

        [Test]
        public async Task RemoveFromApplication()
        {
            var libraryId = new LibraryId(PackageSources.NuGet, "Newtonsoft.Json", "12.0.2");

            _repository
                .Setup(r => r.OpenLibraryFileReadAsync(libraryId, "index.json", CancellationToken.None))
                .ReturnsAsync(new NuGetLibraryIndexJson
                {
                    UsedBy =
                    {
                        new Application { Name = "app1" },
                        new Application { Name = "app2" }
                    }
                }.JsonSerialize);

            _repository
                .Setup(r => r.WriteLibraryFileAsync(libraryId, "index.json", It.IsNotNull<byte[]>(), CancellationToken.None))
                .Returns<LibraryId, string, byte[], CancellationToken>((a, b, content, _) =>
                {
                    var model = content.JsonDeserialize<NuGetLibraryIndexJson>();
                    model.UsedBy.Count.ShouldBe(1);
                    model.UsedBy[0].Name.ShouldBe("app1");
                    return Task.CompletedTask;
                });

            var actual = await _sut.RemoveFromApplicationAsync(libraryId, "app2", CancellationToken.None);

            actual.ShouldBe(PackageRemoveResult.Removed);
            _repository.VerifyAll();
        }

        [Test]
        public async Task RemoveFromApplicationAndStorage()
        {
            var libraryId = new LibraryId(PackageSources.NuGet, "Newtonsoft.Json", "12.0.2");

            _repository
                .Setup(r => r.OpenLibraryFileReadAsync(libraryId, "index.json", CancellationToken.None))
                .ReturnsAsync(new NuGetLibraryIndexJson
                {
                    UsedBy =
                    {
                        new Application { Name = "app1" }
                    }
                }.JsonSerialize);

            _repository
                .Setup(r => r.WriteLibraryFileAsync(libraryId, "index.json", It.IsNotNull<byte[]>(), CancellationToken.None))
                .Returns<LibraryId, string, byte[], CancellationToken>((a, b, content, _) =>
                {
                    var model = content.JsonDeserialize<NuGetLibraryIndexJson>();
                    model.UsedBy.Count.ShouldBe(0);
                    return Task.CompletedTask;
                });

            var actual = await _sut.RemoveFromApplicationAsync(libraryId, "app1", CancellationToken.None);

            actual.ShouldBe(PackageRemoveResult.RemovedNoRefs);
            _repository.VerifyAll();
        }

        [Test]
        public async Task RemoveFromApplicationIgnore()
        {
            var libraryId = new LibraryId(PackageSources.NuGet, "Newtonsoft.Json", "12.0.2");

            _repository
                .Setup(r => r.OpenLibraryFileReadAsync(libraryId, "index.json", CancellationToken.None))
                .ReturnsAsync(new NuGetLibraryIndexJson
                {
                    UsedBy =
                    {
                        new Application { Name = "app1" }
                    }
                }.JsonSerialize);

            var actual = await _sut.RemoveFromApplicationAsync(libraryId, "app2", CancellationToken.None);

            actual.ShouldBe(PackageRemoveResult.None);
            _repository.VerifyAll();
        }
    }
}
