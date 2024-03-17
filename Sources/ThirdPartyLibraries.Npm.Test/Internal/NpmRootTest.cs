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

        Path.GetDirectoryName(actual).ShouldNotBeNullOrWhiteSpace();
        Assert.That(Path.GetDirectoryName(actual), Does.Exist.IgnoreFiles);
    }
}