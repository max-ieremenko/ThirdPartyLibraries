using System.Reflection;
using System.Runtime.Loader;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.PowerShell.Internal;

[TestFixture]
public class DependencyResolverTest
{
    private DependencyResolver _sut = null!;
    private Assembly[] _assembliesBefore = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _sut = new DependencyResolver();
        _assembliesBefore = AppDomain.CurrentDomain.GetAssemblies();
    }

    [TearDown]
    public void AfterEachTest()
    {
        _sut.Dispose();

        var assemblies = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Except(_assembliesBefore)
            .Where(i => i.GetName().Name!.Contains("ThirdPartyLibraries", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        assemblies.ShouldAllBe(i => AssemblyLoadContext.Default != AssemblyLoadContext.GetLoadContext(i));
    }

    [Test]
    public void BindRunAsync()
    {
        _sut.BindRunAsync().ShouldNotBeNull();
    }
}