using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

[TestFixture]
public class LicenseFileNameResolverTest
{
    private LicenseFileNameResolver _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _sut = new LicenseFileNameResolver();
    }

    [Test]
    public void TakeRepositoryLicense()
    {
        _sut.AddFile(new LicenseFile("MIT", "dummy.txt", new ArrayHash(1)));
        _sut.Seal();

        var actual = _sut.ResolveFileSource(new ArrayHash(1));

        actual.LicenseCode.ShouldBe("MIT");
        actual.Library.ShouldBeNull();
        actual.OriginalFileName.ShouldBe("dummy.txt");
        actual.ReportFileName.ShouldBe("MIT-license.txt");
    }

    [Test]
    public void TakeRepositoryLicenseForPackage()
    {
        var package = new LibraryId("dummy", "package", "1.0");
        _sut.AddFile(new PackageLicenseFile(package, "MIT", "dummy.txt", new ArrayHash(1)));
        _sut.AddFile(new LicenseFile("MIT", "dummy.txt", new ArrayHash(1)));
        _sut.Seal();

        var actual = _sut.ResolveFileSource(new ArrayHash(1));

        actual.LicenseCode.ShouldBe("MIT");
        actual.Library.ShouldBeNull();
        actual.OriginalFileName.ShouldBe("dummy.txt");
        actual.ReportFileName.ShouldBe("MIT-license.txt");
    }

    [Test]
    public void TakePackageName()
    {
        var package = new LibraryId("dummy", "package", "1.0");
        _sut.AddFile(new PackageLicenseFile(package, "MIT", "dummy.txt", new ArrayHash(1)));
        _sut.Seal();

        var actual = _sut.ResolveFileSource(new ArrayHash(1));

        actual.LicenseCode.ShouldBeNull();
        actual.Library.ShouldBe(package);
        actual.OriginalFileName.ShouldBe("dummy.txt");
        actual.ReportFileName.ShouldBe("package.txt");
    }

    [Test]
    public void TakePackageNameAndVersion()
    {
        var package1 = new LibraryId("dummy", "package", "1.0");
        var package2 = new LibraryId("dummy", "package", "2.0");

        _sut.AddFile(new PackageLicenseFile(package1, "MIT", "dummy1.txt", new ArrayHash(1)));
        _sut.AddFile(new PackageLicenseFile(package2, "MIT", "dummy2.txt", new ArrayHash(2)));
        _sut.Seal();

        var actual1 = _sut.ResolveFileSource(new ArrayHash(1));
        
        actual1.LicenseCode.ShouldBeNull();
        actual1.Library.ShouldBe(package1);
        actual1.OriginalFileName.ShouldBe("dummy1.txt");
        actual1.ReportFileName.ShouldBe("package-1.0.txt");

        var actual2 = _sut.ResolveFileSource(new ArrayHash(2));

        actual2.LicenseCode.ShouldBeNull();
        actual2.Library.ShouldBe(package2);
        actual2.OriginalFileName.ShouldBe("dummy2.txt");
        actual2.ReportFileName.ShouldBe("package-2.0.txt");
    }

    [Test]
    public void TakeFirstPackageName()
    {
        var package1 = new LibraryId("dummy", "package", "1.0");
        var package2 = new LibraryId("dummy", "package.annotations", "2.0");

        _sut.AddFile(new PackageLicenseFile(package1, "MIT", "dummy1.txt", new ArrayHash(1)));
        _sut.AddFile(new PackageLicenseFile(package2, "MIT", "dummy2.txt", new ArrayHash(1)));
        _sut.Seal();

        var actual = _sut.ResolveFileSource(new ArrayHash(1));

        actual.LicenseCode.ShouldBeNull();
        actual.Library.ShouldBeOneOf(package1, package2);
        actual.OriginalFileName.ShouldBe(actual.Library.Equals(package1) ? "dummy1.txt" : "dummy2.txt");
        actual.ReportFileName.ShouldBe("package.txt");
    }

    [Test]
    public void TakeFirstPackageNameAndVersion()
    {
        var package1 = new LibraryId("dummy", "package", "1.0");
        var package2 = new LibraryId("dummy", "package.annotations", "1.0");
        var package3 = new LibraryId("dummy", "package", "2.0");

        _sut.AddFile(new PackageLicenseFile(package1, "MIT", "dummy1.txt", new ArrayHash(1)));
        _sut.AddFile(new PackageLicenseFile(package2, "MIT", "dummy2.txt", new ArrayHash(1)));
        _sut.AddFile(new PackageLicenseFile(package3, "MIT", "dummy3.txt", new ArrayHash(2)));
        _sut.Seal();

        var actual1 = _sut.ResolveFileSource(new ArrayHash(1));

        actual1.LicenseCode.ShouldBeNull();
        actual1.Library.ShouldBeOneOf(package1, package2);
        actual1.ReportFileName.ShouldBe("package-1.0.txt");

        var actual2 = _sut.ResolveFileSource(new ArrayHash(2));

        actual2.LicenseCode.ShouldBeNull();
        actual2.Library.ShouldBe(package3);
        actual2.OriginalFileName.ShouldBe("dummy3.txt");
        actual2.ReportFileName.ShouldBe("package-2.0.txt");
    }

    [Test]
    public void TakeFirstPackageNameAndVersionAndApplyHash()
    {
        var package1 = new LibraryId("dummy1", "package", "1.0");
        var package2 = new LibraryId("dummy2", "package", "1.0");

        _sut.AddFile(new PackageLicenseFile(package1, "MIT", "dummy1.txt", new ArrayHash(1)));
        _sut.AddFile(new PackageLicenseFile(package2, "MIT", "dummy3.txt", new ArrayHash(2)));
        _sut.Seal();

        var actual1 = _sut.ResolveFileSource(new ArrayHash(1));

        actual1.LicenseCode.ShouldBeNull();
        actual1.Library.ShouldBe(package1);
        actual1.ReportFileName.ShouldBe("package-1.0_010000.txt");

        var actual2 = _sut.ResolveFileSource(new ArrayHash(2));

        actual2.LicenseCode.ShouldBeNull();
        actual2.Library.ShouldBe(package2);
        actual2.ReportFileName.ShouldBe("package-1.0_020000.txt");
    }

    [Test]
    public void TakeLicenseCode()
    {
        var package1 = new LibraryId("dummy", "packageA", "1.0");
        var package2 = new LibraryId("dummy", "packageB", "1.0");

        _sut.AddFile(new PackageLicenseFile(package1, "MIT", "dummy1.txt", new ArrayHash(1)));
        _sut.AddFile(new PackageLicenseFile(package2, "MIT", "dummy2.txt", new ArrayHash(1)));
        _sut.Seal();

        var actual = _sut.ResolveFileSource(new ArrayHash(1));

        actual.LicenseCode.ShouldBeNull();
        actual.Library.ShouldBeOneOf(package1, package2);
        actual.ReportFileName.ShouldBe("MIT.txt");
    }

    [Test]
    public void TakeRepositoryOwner()
    {
        var package1 = new LibraryId("dummy", "packageA", "1.0");
        var package2 = new LibraryId("dummy", "packageB", "1.0");

        var file1 = new PackageLicenseFile(package1, "MIT", "dummy1.txt", new ArrayHash(1))
        {
            RepositoryOwner = "dotnet"
        };
        var file2 = new PackageLicenseFile(package2, "MIT", "dummy2.txt", new ArrayHash(1))
        {
            RepositoryOwner = "dotnet"
        };

        _sut.AddFile(file1);
        _sut.AddFile(file2);
        _sut.Seal();

        var actual = _sut.ResolveFileSource(new ArrayHash(1));

        actual.LicenseCode.ShouldBeNull();
        actual.Library.ShouldBeOneOf(package1, package2);
        actual.ReportFileName.ShouldBe("dotnet.txt");
    }

    [Test]
    public void TakeRepositoryName()
    {
        var package1 = new LibraryId("dummy", "packageA", "1.0");
        var package2 = new LibraryId("dummy", "packageB", "1.0");

        var file1 = new PackageLicenseFile(package1, "MIT", "dummy1.txt", new ArrayHash(1))
        {
            RepositoryName = "runtime"
        };
        var file2 = new PackageLicenseFile(package2, "MIT", "dummy2.txt", new ArrayHash(1))
        {
            RepositoryName = "runtime"
        };

        _sut.AddFile(file1);
        _sut.AddFile(file2);
        _sut.Seal();

        var actual = _sut.ResolveFileSource(new ArrayHash(1));

        actual.LicenseCode.ShouldBeNull();
        actual.Library.ShouldBeOneOf(package1, package2);
        actual.ReportFileName.ShouldBe("runtime.txt");
    }

    [Test]
    public void TakeRepositoryOwnerAndName()
    {
        var package1 = new LibraryId("dummy", "packageA", "1.0");
        var package2 = new LibraryId("dummy", "packageB", "1.0");
        var package3 = new LibraryId("dummy", "packageC", "1.0");
        var package4 = new LibraryId("dummy", "packageD", "1.0");

        var file1 = new PackageLicenseFile(package1, "MIT", "dummy1.txt", new ArrayHash(1))
        {
            RepositoryOwner = "dotnet",
            RepositoryName = "runtime"
        };
        var file2 = new PackageLicenseFile(package2, "MIT", "dummy2.txt", new ArrayHash(1))
        {
            RepositoryOwner = "dotnet",
            RepositoryName = "runtime"
        };
        var file3 = new PackageLicenseFile(package3, "MIT", "dummy2.txt", new ArrayHash(2))
        {
            RepositoryOwner = "dotnet",
            RepositoryName = "corefx"
        };
        var file4 = new PackageLicenseFile(package4, "MIT", "dummy2.txt", new ArrayHash(2))
        {
            RepositoryOwner = "dotnet",
            RepositoryName = "corefx"
        };

        _sut.AddFile(file1);
        _sut.AddFile(file2);
        _sut.AddFile(file3);
        _sut.AddFile(file4);
        _sut.Seal();

        var actual1 = _sut.ResolveFileSource(new ArrayHash(1));

        actual1.LicenseCode.ShouldBeNull();
        actual1.Library.ShouldBeOneOf(package1, package2);
        actual1.ReportFileName.ShouldBe("dotnet-runtime.txt");

        var actual2 = _sut.ResolveFileSource(new ArrayHash(2));

        actual2.LicenseCode.ShouldBeNull();
        actual2.Library.ShouldBeOneOf(package3, package4);
        actual2.ReportFileName.ShouldBe("dotnet-corefx.txt");
    }

    [Test]
    public void AppendFileUniqueIndex()
    {
        _sut.AddFile(new LicenseFile("MIT", "dummy1.txt", new ArrayHash(1)));
        _sut.AddFile(new LicenseFile("MIT", "dummy2.txt", new ArrayHash(2)));
        _sut.Seal();

        var actual1 = _sut.ResolveFileSource(new ArrayHash(1));

        actual1.LicenseCode.ShouldBe("MIT");
        actual1.Library.ShouldBeNull();
        actual1.OriginalFileName.ShouldBe("dummy1.txt");
        actual1.ReportFileName.ShouldBe("MIT-license_010000.txt");

        var actual2 = _sut.ResolveFileSource(new ArrayHash(2));

        actual2.LicenseCode.ShouldBe("MIT");
        actual2.Library.ShouldBeNull();
        actual2.OriginalFileName.ShouldBe("dummy2.txt");
        actual2.ReportFileName.ShouldBe("MIT-license_020000.txt");
    }
}