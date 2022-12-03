using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters;

[TestFixture]
public class NuGetIgnoreFilterTest
{
    [Test]
    [TestCase(null, "name", false)]
    [TestCase("n.*", "name", true)]
    [TestCase("x.*", "name", false)]
    public void FilterByName(string pattern, string name, bool expected)
    {
        var configuration = new NuGetIgnoreFilterConfiguration();
        if (pattern != null)
        {
            configuration.ByName = new[] { pattern };
        }

        var sut = new NuGetIgnoreFilter(configuration);
        sut.FilterByName(name).ShouldBe(expected);
    }

    [Test]
    [TestCase(null, "name", false)]
    [TestCase("n.*", "name", true)]
    [TestCase("x.*", "name", false)]
    public void FilterByProjectName(string pattern, string name, bool expected)
    {
        var configuration = new NuGetIgnoreFilterConfiguration();
        if (pattern != null)
        {
            configuration.ByProjectName = new[] { pattern };
        }

        var sut = new NuGetIgnoreFilter(configuration);
        sut.FilterByProjectName(name).ShouldBe(expected);
    }
}