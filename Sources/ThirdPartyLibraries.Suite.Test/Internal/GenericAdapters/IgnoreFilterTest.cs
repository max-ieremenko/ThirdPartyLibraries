using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Suite.Internal.GenericAdapters
{
    [TestFixture]
    public class IgnoreFilterTest
    {
        [Test]
        [TestCase(null, "name", false)]
        [TestCase("n.*", "name", true)]
        [TestCase("x.*", "name", false)]
        public void Filter(string pattern, string name, bool expected)
        {
            var sut = new IgnoreFilter(pattern == null ? null : new[] { pattern });
            sut.Filter(name).ShouldBe(expected);
        }
    }
}
