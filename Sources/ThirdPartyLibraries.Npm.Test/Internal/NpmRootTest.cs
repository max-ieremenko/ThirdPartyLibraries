using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Npm.Internal;

[TestFixture]
public class NpmRootTest
{
    [Test]
    public void Resolve()
    {
        var actual = NpmRoot.Resolve();

        Console.WriteLine(actual);
        actual.ShouldNotBeNull();
    }
}