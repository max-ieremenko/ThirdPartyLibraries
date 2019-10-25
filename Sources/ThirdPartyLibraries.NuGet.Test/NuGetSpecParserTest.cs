using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.NuGet
{
    [TestFixture]
    public class NuGetSpecParserTest
    {
        [Test]
        [TestCase(".NETStandard2.0", new[] { "fallback group", "no group" })]
        [TestCase(".NETFramework2.0", new[] { "no group" })]
        [TestCase(".NETFramework4.5", new[] { "45", "no group" })]
        public void ExtractDependencies(string targetFramework, string[] expected)
        {
            var dependenciesByTargetFramework = new Dictionary<string, NuGetPackageId[]>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    NuGetSpecParser.NoGroupTargetFramework,
                    new[] { new NuGetPackageId("no group", "1.0") }
                },
                {
                    ".NETFramework2.0",
                    Array.Empty<NuGetPackageId>()
                },
                {
                    ".NETFramework4.5",
                    new[] { new NuGetPackageId("45", "1.0") }
                },
                {
                    NuGetSpecParser.FallbackGroupTargetFramework,
                    new[] { new NuGetPackageId("fallback group", "1.0") }
                },
            };

            var expectedIds = expected.Select(i => new NuGetPackageId(i, "1.0")).ToArray();
            var actual = NuGetSpecParser.ExtractDependencies(dependenciesByTargetFramework, targetFramework).ToArray();

            actual.ShouldBe(expectedIds, true);
        }
    }
}
