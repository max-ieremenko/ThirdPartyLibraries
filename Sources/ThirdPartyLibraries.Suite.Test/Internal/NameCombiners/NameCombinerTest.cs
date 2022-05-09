using System.Collections.Generic;
using System.Linq;
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
    [TestCaseSource(nameof(GetTestCases))]
    public void Test(object[] groups, string[] expectedFileNames)
    {
        _sut.Initialize(groups.Cast<NamesGroup>());

        for (var i = 0; i < groups.Length; i++)
        {
            var group = (NamesGroup)groups[i];
            _sut.GetFileName(group).ShouldBe(expectedFileNames[i]);
        }
    }

    private static IEnumerable<TestCaseData> GetTestCases()
    {
        // test case
        var groups = new object[]
        {
            new NamesGroup("MIT", ".txt", "MIT-license")
            {
                Names = { new Name("package", "1.0") }
            }
        };
        var expectedFileNames = new[] { "MIT-license.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take repository license" };

        // test case
        groups = new object[]
        {
            new NamesGroup("MIT", ".txt", null)
            {
                Names = { new Name("package", "1.0") }
            }
        };
        expectedFileNames = new[] { "package.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take package name" };

        // test case
        groups = new object[]
        {
            new NamesGroup("MIT", ".txt", null)
            {
                Names = { new Name("package", "1.0") }
            },
            new NamesGroup("MIT", ".txt", null)
            {
                Names = { new Name("package", "2.0") }
            }
        };
        expectedFileNames = new[] { "package-1.0.txt", "package-2.0.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take package name and version" };

        // test case
        groups = new object[]
        {
            new NamesGroup("MIT", ".txt", null)
            {
                Names =
                {
                    new Name("package", "1.0"),
                    new Name("package.annotations", "1.0")
                }
            }
        };
        expectedFileNames = new[] { "package.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take first package name" };

        // test case
        groups = new object[]
        {
            new NamesGroup("MIT", ".txt", null)
            {
                Names =
                {
                    new Name("package", "1.0"),
                    new Name("package.annotations", "1.0")
                }
            },
            new NamesGroup("MIT", ".txt", null)
            {
                Names =
                {
                    new Name("package", "2.0")
                }
            }
        };
        expectedFileNames = new[] { "package-1.0.txt", "package-2.0.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take first package name and version" };

        // test case
        groups = new object[]
        {
            new NamesGroup("MIT", ".txt", null)
            {
                Names =
                {
                    new Name("package", "1.0"),
                    new Name("package.annotations", "1.0")
                }
            },
            new NamesGroup("MIT", ".txt", null)
            {
                Names =
                {
                    new Name("package", "1.0")
                }
            }
        };
        expectedFileNames = new[] { "package-1.0-1.txt", "package-1.0-2.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take first package name and version and apply index" };

        // test case
        groups = new object[]
        {
            new NamesGroup("MIT", ".txt", null)
            {
                Names =
                {
                    new Name("packageA", "1.0"),
                    new Name("packageB.annotations", "1.0")
                }
            }
        };
        expectedFileNames = new[] { "MIT.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take license code" };

        // test case
        groups = new object[]
        {
            new NamesGroup("MIT", ".txt", null)
            {
                Names =
                {
                    new Name("packageA", "1.0"),
                    new Name("packageB.annotations", "1.0")
                }
            },
            new NamesGroup("MIT", ".txt", null)
            {
                Names =
                {
                    new Name("packageC", "1.0"),
                    new Name("packageG.annotations", "1.0")
                }
            }
        };
        expectedFileNames = new[] { "MIT-1.txt", "MIT-2.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take license code with index" };

        // test case
        groups = new object[]
        {
            new NamesGroup("MIT", ".txt", null)
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
        expectedFileNames = new[] { "MIT-dotnet.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take first alternative name" };

        // test case
        groups = new object[]
        {
            new NamesGroup("MIT", ".txt", null)
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
            new NamesGroup("MIT", ".txt", null)
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
        expectedFileNames = new[] { "MIT-dotnet-runtime.txt", "MIT-dotnet-corefx.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take first and second alternative name" };

        // test case
        groups = new object[]
        {
            new NamesGroup("MIT", ".txt", null)
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
            new NamesGroup("MIT", ".txt", null)
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
        expectedFileNames = new[] { "MIT-dotnet-runtime-1.txt", "MIT-dotnet-runtime-2.txt" };

        yield return new TestCaseData(groups, expectedFileNames) { TestName = "take first and second alternative name with index" };
    }
}