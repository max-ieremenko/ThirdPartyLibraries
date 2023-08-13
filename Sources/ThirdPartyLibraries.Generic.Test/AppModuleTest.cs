using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Generic.Internal;

namespace ThirdPartyLibraries.Generic;

[TestFixture]
public class AppModuleTest
{
    private ServiceProvider _serviceProvider = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddSingleton<Func<HttpClient>>(() => throw new NotSupportedException());

        AppModule.ConfigureServices(services, configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void AfterEachTest()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public void ResolveOpenSourceLicenseLoader()
    {
        var byUrlLoaders = _serviceProvider.GetServices<ILicenseByUrlLoader>().OfType<OpenSourceLicenseLoader>().ToArray();
        byUrlLoaders.Length.ShouldBe(1);

        var byCodeLoaders = _serviceProvider.GetServices<ILicenseByCodeLoader>().OfType<OpenSourceLicenseLoader>().ToArray();
        byCodeLoaders.Length.ShouldBe(1);

        var loader = _serviceProvider.GetRequiredService<OpenSourceLicenseLoader>();

        ReferenceEquals(byUrlLoaders[0], loader).ShouldBeTrue();
        ReferenceEquals(byCodeLoaders[0], loader).ShouldBeTrue();
    }
}