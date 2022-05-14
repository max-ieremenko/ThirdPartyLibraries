using System;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Suite.Internal.NameCombiners;

[TestFixture]
public class NameCombinerTest
{
    private NameCombiner _sut;

    [SetUp]
    public void BeforeEachTest()
    {
        _sut = new NameCombiner();
    }

    [Test]
    public void TakeRepositoryLicense()
    {
        var groups = new[]
        {
            new NamesGroup("MIT", Array.Empty<byte>(), ".txt", "MIT-license")
            {
                Names = { new Name("package", "1.0") }
            }
        };
        var expectedFileNames = new[] { "MIT-license.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void TakePackageName()
    {
        // test case
        var groups = new[]
        {
            new NamesGroup("MIT", Array.Empty<byte>(), ".txt", null)
            {
                Names = { new Name("package", "1.0") }
            }
        };
        var expectedFileNames = new[] { "package.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void TakePackageNameAndVersion()
    {
        var groups = new[]
        {
            new NamesGroup("MIT", Array.Empty<byte>(), ".txt", null)
            {
                Names = { new Name("package", "1.0") }
            },
            new NamesGroup("MIT", Array.Empty<byte>(), ".txt", null)
            {
                Names = { new Name("package", "2.0") }
            }
        };
        var expectedFileNames = new[] { "package-1.0.txt", "package-2.0.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void TakeFirstPackageName()
    {
        var groups = new[]
        {
            new NamesGroup("MIT", Array.Empty<byte>(), ".txt", null)
            {
                Names =
                {
                    new Name("package", "1.0"),
                    new Name("package.annotations", "1.0")
                }
            }
        };
        var expectedFileNames = new[] { "package.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void TakeFirstPackageNameAndVersion()
    {
        // test case
        var groups = new[]
        {
            new NamesGroup("MIT", Array.Empty<byte>(), ".txt", null)
            {
                Names =
                {
                    new Name("package", "1.0"),
                    new Name("package.annotations", "1.0")
                }
            },
            new NamesGroup("MIT", Array.Empty<byte>(), ".txt", null)
            {
                Names =
                {
                    new Name("package", "2.0")
                }
            }
        };
        var expectedFileNames = new[] { "package-1.0.txt", "package-2.0.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void TakeFirstPackageNameAndVersionAndApplyIndex()
    {
        // test case
        var groups = new[]
        {
            new NamesGroup("MIT", new byte[] { 1 }, ".txt", null)
            {
                Names =
                {
                    new Name("package", "1.0"),
                    new Name("package.annotations", "1.0")
                }
            },
            new NamesGroup("MIT", new byte[] { 2 }, ".txt", null)
            {
                Names =
                {
                    new Name("package", "1.0")
                }
            }
        };
        var expectedFileNames = new[] { "package-1.0_01.txt", "package-1.0_02.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void TakeLicenseCode()
    {
        var groups = new[]
        {
            new NamesGroup("MIT", new byte[] { 1, 1, 1, 1 }, ".txt", null)
            {
                Names =
                {
                    new Name("packageA", "1.0"),
                    new Name("packageB.annotations", "1.0")
                }
            }
        };
        var expectedFileNames = new[] { "MIT_01010101.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void TakeLicenseCodeWithIndex()
    {
        var groups = new[]
        {
            new NamesGroup("MIT", new byte[] { 1, 1, 1, 1 }, ".txt", null)
            {
                Names =
                {
                    new Name("packageA", "1.0"),
                    new Name("packageB.annotations", "1.0")
                }
            },
            new NamesGroup("MIT", new byte[] { 2, 2, 2, 2 }, ".txt", null)
            {
                Names =
                {
                    new Name("packageC", "1.0"),
                    new Name("packageG.annotations", "1.0")
                }
            }
        };
        var expectedFileNames = new[] { "MIT_01010101.txt", "MIT_02020202.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void TakeFirstAlternativeName()
    {
        // test case
        var groups = new[]
        {
            new NamesGroup("MIT", Array.Empty<byte>(), ".txt", null)
            {
                Names =
                {
                    new Name("packageA", "1.0"),
                    new Name("packageB.annotations", "1.0")
                },
                AlternativeNames =
                {
                    new Name("dotnet", "runtime"),
                    new Name("dotnet", "corefx")
                }
            }
        };
        var expectedFileNames = new[] { "MIT-dotnet.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void TakeFirstAndSecondAlternativeName()
    {
        var groups = new[]
        {
            new NamesGroup("MIT", Array.Empty<byte>(), ".txt", null)
            {
                Names =
                {
                    new Name("packageA", "1.0"),
                    new Name("packageB.annotations", "1.0")
                },
                AlternativeNames =
                {
                    new Name("dotnet", "runtime")
                }
            },
            new NamesGroup("MIT", Array.Empty<byte>(), ".txt", null)
            {
                Names =
                {
                    new Name("packageA", "2.0"),
                    new Name("packageB.annotations", "2.0")
                },
                AlternativeNames =
                {
                    new Name("dotnet", "corefx")
                }
            }
        };
        var expectedFileNames = new[] { "MIT-dotnet-runtime.txt", "MIT-dotnet-corefx.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void TakeFirstAndSecondAlternativeNameWithIndex()
    {
        // test case
        var groups = new[]
        {
            new NamesGroup("MIT", new byte[] { 1 }, ".txt", null)
            {
                Names =
                {
                    new Name("packageA", "1.0"),
                    new Name("packageB.annotations", "1.0")
                },
                AlternativeNames =
                {
                    new Name("dotnet", "runtime")
                }
            },
            new NamesGroup("MIT", new byte[] { 2 }, ".txt", null)
            {
                Names =
                {
                    new Name("packageA", "2.0"),
                    new Name("packageB.annotations", "2.0")
                },
                AlternativeNames =
                {
                    new Name("dotnet", "runtime")
                }
            }
        };
        var expectedFileNames = new[] { "MIT-dotnet-runtime_01.txt", "MIT-dotnet-runtime_02.txt" };

        RunTest(groups, expectedFileNames);
    }

    [Test]
    public void AppendFileUniqueIndex()
    {
        // test case
        var groups = new[]
        {
            new NamesGroup("MIT", new byte[] { 1 }, ".txt", "MIT"),
            new NamesGroup("MIT", new byte[] { 2 }, ".txt", "MIT")
        };
        var expectedFileNames = new[] { "MIT_01.txt", "MIT_02.txt" };

        RunTest(groups, expectedFileNames);
    }

    private void RunTest(NamesGroup[] groups, string[] expectedFileNames)
    {
        _sut.Initialize(groups);

        var actualFileNames = new string[groups.Length];
        for (var i = 0; i < groups.Length; i++)
        {
            actualFileNames[i] = _sut.GetFileName(groups[i]);
        }

        actualFileNames.ShouldBe(expectedFileNames);
    }
}