using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.NuGet;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    [TestFixture]
    public class NuGetPackageResolverTest
    {
        private Mock<INuGetApi> _nuGetApi;
        private Mock<ILicenseResolver> _licenseResolver;
        private NuGetPackageResolver _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            _nuGetApi = new Mock<INuGetApi>(MockBehavior.Strict);
            _licenseResolver = new Mock<ILicenseResolver>(MockBehavior.Strict);

            _sut = new NuGetPackageResolver
            {
                NuGetApi = _nuGetApi.Object,
                LicenseResolver = _licenseResolver.Object,
                Configuration = new NuGetConfiguration { AllowToUseLocalCache = true }
            };
        }

        [Test]
        public async Task DownloadNUnit()
        {
            /* https://api.nuget.org/v3-flatcontainer/NUnit/3.12.0/NUnit.nuspec
             * <license type="file">LICENSE.txt</license>
             * <licenseUrl>https://aka.ms/deprecateLicenseUrl</licenseUrl>
             * <projectUrl>https://nunit.org/</projectUrl>
             */

            var id = new LibraryId(PackageSources.NuGet, "NUnit", "3.12.0");

            SetupLoadSpecAsync(
                id.Name,
                id.Version,
                new NuGetSpec
                {
                    Id = id.Name,
                    Version = id.Version,
                    PackageHRef = "https://www.nuget.org/packages/NUnit/3.12.0",
                    License = new NuGetSpecLicense { Type = "file", Value = "LICENSE.txt" },
                    LicenseUrl = "https://aka.ms/deprecateLicenseUrl",
                    ProjectUrl = "https://nunit.org/"
                });

            _nuGetApi
                .Setup(a => a.LoadFileContentAsync(new NuGetPackageId(id.Name, id.Version), "LICENSE.txt", true, CancellationToken.None))
                .ReturnsAsync("license text".AsBytes);

            _licenseResolver
                .Setup(l => l.ResolveByUrlAsync("https://nunit.org/", CancellationToken.None))
                .ReturnsAsync((LicenseInfo)null);

            var actual = await _sut.DownloadAsync(id, CancellationToken.None);

            actual.ShouldNotBeNull();
            actual.SourceCode.ShouldBe(PackageSources.NuGet);
            actual.Name.ShouldBe("NUnit");
            actual.Version.ShouldBe("3.12.0");
            actual.LicenseCode.ShouldBeNull();

            actual.Licenses.Count.ShouldBe(2);

            actual.Licenses[0].Subject.ShouldBe(NuGetPackageResolver.LicenseSubjectPackage);
            actual.Licenses[0].Code.ShouldBeNull();
            actual.Licenses[0].CodeDescription.ShouldBeNull();
            actual.Licenses[0].HRef.ShouldBeNull();

            actual.Licenses[1].Subject.ShouldBe(NuGetPackageResolver.LicenseSubjectProject);
            actual.Licenses[1].Code.ShouldBeNull();
            actual.Licenses[1].CodeDescription.ShouldNotBeNull();
            actual.Licenses[1].HRef.ShouldBe("https://nunit.org/");

            actual.Attachments.Count.ShouldBe(2);

            actual.Attachments[0].Name.ShouldBe("package.nuspec");
            actual.Attachments[0].Content.ShouldNotBeNull();
            
            actual.Attachments[1].Name.ShouldBe("package-LICENSE.txt");
            actual.Attachments[1].Content.AsText().ShouldBe("license text");
        }

        [Test]
        public async Task DownloadNewtonsoftJson()
        {
            /* https://api.nuget.org/v3-flatcontainer/Newtonsoft.Json/12.0.2/Newtonsoft.Json.nuspec
             * <license type="expression">MIT</license>
             * <licenseUrl>https://licenses.nuget.org/MIT</licenseUrl>
             * <projectUrl>https://www.newtonsoft.com/json</projectUrl>
             * <repository type="git" url="https://github.com/JamesNK/Newtonsoft.Json" commit="4ab34b0461fb595805d092a46a58f35f66c84d6a" />
             */

            var id = new LibraryId(PackageSources.NuGet, "Newtonsoft.Json", "12.0.2");
            
            SetupLoadSpecAsync(
                id.Name,
                id.Version,
                new NuGetSpec
                {
                    Id = id.Name,
                    Version = id.Version,
                    PackageHRef = "https://www.nuget.org/packages/Newtonsoft.Json/12.0.2",
                    License = new NuGetSpecLicense { Type = "expression", Value = "MIT" },
                    LicenseUrl = "https://licenses.nuget.org/MIT",
                    ProjectUrl = "https://www.newtonsoft.com/json",
                    Repository = new NuGetSpecRepository
                    {
                        Type = "git",
                        Url = "https://github.com/JamesNK/Newtonsoft.Json"
                    }
                });

            _nuGetApi
                .Setup(a => a.TryFindLicenseFileAsync(new NuGetPackageId(id.Name, id.Version), true, CancellationToken.None))
                .ReturnsAsync(new NuGetPackageLicenseFile
                {
                    Name = "LICENSE.md",
                    Content = "file license text".AsBytes()
                });

            _licenseResolver
                .Setup(l => l.ResolveByUrlAsync("https://licenses.nuget.org/MIT", CancellationToken.None))
                .ReturnsAsync(new LicenseInfo { Code = "MIT" });

            _licenseResolver
                .Setup(l => l.ResolveByUrlAsync("https://github.com/JamesNK/Newtonsoft.Json", CancellationToken.None))
                .ReturnsAsync(new LicenseInfo
                {
                    Code = "MIT",
                    FileName = "LICENSE.md",
                    FileContent = "git hub license text".AsBytes(),
                    FileHRef = "https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/master/LICENSE.md"
                });

            _licenseResolver
                .Setup(l => l.ResolveByUrlAsync("https://www.newtonsoft.com/json", CancellationToken.None))
                .ReturnsAsync((LicenseInfo)null);

            var actual = await _sut.DownloadAsync(id, CancellationToken.None);

            actual.ShouldNotBeNull();
            actual.SourceCode.ShouldBe(PackageSources.NuGet);
            actual.Name.ShouldBe("Newtonsoft.Json");
            actual.Version.ShouldBe("12.0.2");
            actual.LicenseCode.ShouldBe("MIT");

            actual.Licenses.Count.ShouldBe(3);

            actual.Licenses[0].Subject.ShouldBe(NuGetPackageResolver.LicenseSubjectPackage);
            actual.Licenses[0].Code.ShouldBe("MIT");
            actual.Licenses[0].CodeDescription.ShouldBeNull();
            actual.Licenses[0].HRef.ShouldBe("https://licenses.nuget.org/MIT");
            
            actual.Licenses[1].Subject.ShouldBe(NuGetPackageResolver.LicenseSubjectRepository);
            actual.Licenses[1].Code.ShouldBe("MIT");
            actual.Licenses[1].CodeDescription.ShouldBeNull();
            actual.Licenses[1].HRef.ShouldBe("https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/master/LICENSE.md");

            actual.Licenses[2].Subject.ShouldBe(NuGetPackageResolver.LicenseSubjectProject);
            actual.Licenses[2].Code.ShouldBeNull();
            actual.Licenses[2].CodeDescription.ShouldNotBeNull();
            actual.Licenses[2].HRef.ShouldBe("https://www.newtonsoft.com/json");

            actual.Attachments.Count.ShouldBe(3);

            actual.Attachments[0].Name.ShouldBe("package.nuspec");
            actual.Attachments[0].Content.ShouldNotBeNull();

            actual.Attachments[1].Name.ShouldBe("package-LICENSE.md");
            actual.Attachments[1].Content.AsText().ShouldBe("file license text");

            actual.Attachments[2].Name.ShouldBe("repository-LICENSE.md");
            actual.Attachments[2].Content.AsText().ShouldBe("git hub license text");
        }

        private void SetupLoadSpecAsync(string packageName, string packageVersion, NuGetSpec result)
        {
            var content = new byte[] { 1, 2, 3 };
            _nuGetApi
                .Setup(a => a.LoadSpecAsync(new NuGetPackageId(packageName, packageVersion), true, CancellationToken.None))
                .ReturnsAsync(content);

            _nuGetApi
                .Setup(a => a.ParseSpec(It.IsNotNull<MemoryStream>()))
                .Returns(result);
        }
    }
}
