using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

[TestFixture]
public class LicenseFileNameResolverToolsTest
{
    [Test]
    [TestCaseSource(nameof(GetTryGroupByPackageNameCases))]
    public void TryGroupByPackageName(string[] names, string? expected)
    {
        var actual = LicenseFileNameResolverTools.TryFindCommonName(names);

        actual.ShouldBe(expected);
    }

    [Test]
    [TestCaseSource(nameof(GetMakeUniqueCases))]
    public void MakeUnique(object names, string[] expected)
    {
        var input = (List<(FileNameBuilder, int?)>)names;
        input.MakeUnique();
        
        var actual = input.Select(i => i.Item1.ToString()).ToArray();
        actual.ShouldBe(expected);
    }

    private static IEnumerable<TestCaseData> GetTryGroupByPackageNameCases()
    {
        yield return new TestCaseData(new[] { "System", "System" }, "System")
        {
            TestName = "same name"
        };

        yield return new TestCaseData(new[] { "System.Data.SqlClient", "System.Data.OraClient", "System.Data.NpgClient" }, "System.Data")
        {
            TestName = "same group"
        };

        yield return new TestCaseData(new[] { "System.DataA.SqlClient", "System.DataB.OraClient", "System.DataC.NpgClient" }, "System")
        {
            TestName = "same base group 1"
        };

        yield return new TestCaseData(new[] { "System.Data.SqlClient", "System.Data.OraClient", "System.Data" }, "System.Data")
        {
            TestName = "same base group 2"
        };

        yield return new TestCaseData(new[] { "SystemA.Data", "SystemB.Data" }, null)
        {
            TestName = "no base group"
        };
    }

    private static IEnumerable<TestCaseData> GetMakeUniqueCases()
    {
        var names = new List<(FileNameBuilder, int?)>
        {
            (new FileNameBuilder("packageA", null, null, new ArrayHash(1)), null),
            (new FileNameBuilder("packageB", null, null, new ArrayHash(1)), null)
        };
        yield return new TestCaseData(names, new[] { "packageA", "packageB" })
        {
            TestName = "unique"
        };

        names = new List<(FileNameBuilder, int?)>
        {
            (new FileNameBuilder("package", "1.0", null, new ArrayHash(1)), null),
            (new FileNameBuilder("package", "2.0", null, new ArrayHash(1)), null)
        };
        yield return new TestCaseData(names, new[] { "package-1.0", "package-2.0" })
        {
            TestName = "version"
        };

        names = new List<(FileNameBuilder, int?)>
        {
            (new FileNameBuilder("package", "1.0", ".txt", new ArrayHash(1)), null),
            (new FileNameBuilder("package", "1.0", ".txt", new ArrayHash(2)), null)
        };
        yield return new TestCaseData(names, new[] { "package-1.0_010000.txt", "package-1.0_020000.txt" })
        {
            TestName = "hash"
        };
    }
}