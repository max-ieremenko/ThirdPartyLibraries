using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Repository.Template;

namespace ThirdPartyLibraries.Suite.Update.Internal;

[TestFixture]
internal class LibraryIndexJsonChangeTrackerTest
{
    [Test]
    [TestCaseSource(nameof(GetIsChangedCases))]
    public void IsChanged(LibraryIndexJson? original, LibraryIndexJson index, bool expected)
    {
        LibraryIndexJsonChangeTracker.IsChanged(original, index).ShouldBe(expected);
    }

    private static IEnumerable<TestCaseData> GetIsChangedCases()
    {
        yield return new TestCaseData(null, new LibraryIndexJson(), true)
        {
            TestName = "null one"
        };
        yield return new TestCaseData(new LibraryIndexJson(), new LibraryIndexJson(), false)
        {
            TestName = "empty one"
        };

        yield return new TestCaseData(
            new LibraryIndexJson
            {
                Source = null
            },
            new LibraryIndexJson
            {
                Source = string.Empty
            },
            false)
        {
            TestName = "Source null vs empty string"
        };

        yield return new TestCaseData(
            new LibraryIndexJson
            {
                License = { Code = null }
            },
            new LibraryIndexJson
            {
                License = { Code = string.Empty }
            },
            false)
        {
            TestName = "License.Code null vs empty string"
        };

        yield return new TestCaseData(
            new LibraryIndexJson
            {
                License = { Status = null }
            },
            new LibraryIndexJson
            {
                License = { Status = string.Empty }
            },
            false)
        {
            TestName = "License.Status null vs empty string"
        };

        yield return new TestCaseData(
            new LibraryIndexJson
            {
                UsedBy =
                {
                    new()
                }
            },
            new LibraryIndexJson
            {
                UsedBy =
                {
                    new() { Dependencies = [] }
                }
            },
            false)
        {
            TestName = "UsedBy.Dependencies null vs empty array"
        };

        yield return new TestCaseData(
            new LibraryIndexJson
            {
                UsedBy =
                {
                    new()
                }
            },
            new LibraryIndexJson
            {
                UsedBy =
                {
                    new() { TargetFrameworks = [] }
                }
            },
            false)
        {
            TestName = "UsedBy.TargetFrameworks null vs empty array"
        };

        yield return new TestCaseData(
            new LibraryIndexJson
            {
                Licenses =
                {
                    new()
                }
            },
            new LibraryIndexJson
            {
                Licenses =
                {
                    new() { Code = string.Empty }
                }
            },
            false)
        {
            TestName = "Licenses.Code null vs empty string"
        };

        yield return new TestCaseData(
            new LibraryIndexJson
            {
                Licenses =
                {
                    new()
                }
            },
            new LibraryIndexJson
            {
                Licenses =
                {
                    new() { Description = string.Empty }
                }
            },
            false)
        {
            TestName = "Licenses.Description null vs empty string"
        };

        yield return new TestCaseData(
            new LibraryIndexJson
            {
                Licenses =
                {
                    new()
                }
            },
            new LibraryIndexJson
            {
                Licenses =
                {
                    new() { HRef = string.Empty }
                }
            },
            false)
        {
            TestName = "Licenses.HRef null vs empty string"
        };
    }
}