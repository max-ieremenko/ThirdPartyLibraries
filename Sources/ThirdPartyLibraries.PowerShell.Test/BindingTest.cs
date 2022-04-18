using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.PowerShell;

[TestFixture]
public class BindingTest
{
    [Test]
    public void NoReferencesToThirdPartyLibraries()
    {
        var referencedAssemblies = typeof(CommandCmdlet)
            .Assembly
            .GetReferencedAssemblies()
            .Select(i => i.Name)
            .ToArray();

        referencedAssemblies.ShouldBe(
            new[] { "netstandard", "System.Management.Automation", "System.Runtime.Loader" }, ignoreOrder:
            true);
    }
}