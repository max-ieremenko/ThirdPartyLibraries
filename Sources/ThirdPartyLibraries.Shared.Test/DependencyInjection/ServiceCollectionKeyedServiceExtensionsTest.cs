using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Shared.DependencyInjection;

[TestFixture]
public partial class ServiceCollectionKeyedServiceExtensionsTest
{
    private IServiceCollection _services;

    [SetUp]
    public void BeforeEachTest()
    {
        _services = new ServiceCollection();
    }

    [Test]
    public void GetKeyedTransientService()
    {
        _services.AddKeyedTransient<IService, Service1>("1", _ => new Service1 { Foo = "foo" });
        _services.AddKeyedTransient<IService, Service2>("2");

        var provider = _services.BuildServiceProvider();

        provider.GetServices<IService>().ShouldBeEmpty();

        var actual1 = provider.GetKeyedService<IService>("1").ShouldBeOfType<Service1>();
        actual1.Foo.ShouldBe("foo");

        var actual2 = provider.GetKeyedService<IService>("2").ShouldBeOfType<Service2>();

        ReferenceEquals(provider.GetKeyedService<IService>("1"), actual1).ShouldBeFalse();
        ReferenceEquals(provider.GetKeyedService<IService>("2"), actual2).ShouldBeFalse();
    }

    [Test]
    public void AddKeyedTransientDifferentKeySameService()
    {
        _services.AddKeyedTransient<IService, Service1>("1");
        _services.AddKeyedTransient<IService, Service1>("2");

        var provider = _services.BuildServiceProvider();

        provider.GetRequiredKeyedService<IService>("1").ShouldBeOfType<Service1>();
        provider.GetRequiredKeyedService<IService>("2").ShouldBeOfType<Service1>();
    }

    [Test]
    public void GetKeyedTransientServiceDefault()
    {
        var provider = _services.BuildServiceProvider();

        provider.GetKeyedService<IService>("1").ShouldBeNull();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedService<IService>("1"));
    }

    [Test]
    public void MapToSingleton()
    {
        _services.AddSingleton<Service1>();
        _services.AddKeyedTransient<IService, Service1>("1", p => p.GetRequiredService<Service1>());
        _services.AddKeyedTransient<IService, Service1>("2", p => p.GetRequiredService<Service1>());

        var provider = _services.BuildServiceProvider();

        var actual1 = provider.GetRequiredKeyedService<IService>("1");
        var actual2 = provider.GetRequiredKeyedService<IService>("2");
        ReferenceEquals(actual1, actual2).ShouldBeTrue();
        ReferenceEquals(actual1, provider.GetRequiredService<Service1>()).ShouldBeTrue();
    }
}