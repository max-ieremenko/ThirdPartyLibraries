using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite;

[TestFixture]
public class AppModuleTest
{
    private IServiceCollection _services;
    private Mock<IConfigurationManager> _configurationManager;

    [SetUp]
    public void BeforeEachTest()
    {
        _services = new ServiceCollection();

        _configurationManager = new Mock<IConfigurationManager>(MockBehavior.Strict);
        _services.AddTransient(_ => _configurationManager.Object);

        AppModule.ConfigureServices(_services);
    }

    [Test]
    public void HttpClientFactoryIsTransient()
    {
        using var provider = _services.BuildServiceProvider();

        _configurationManager
            .Setup(m => m.GetSection<SkipCertificateCheckConfiguration>(SkipCertificateCheckConfiguration.SectionName))
            .Returns(new SkipCertificateCheckConfiguration());

        var actual = provider.GetRequiredService<Func<HttpClient>>();

        _configurationManager.VerifyAll();

        var instance1 = actual();
        instance1.ShouldNotBeNull();

        var instance2 = actual();
        instance2.ShouldNotBeNull();

        ReferenceEquals(instance1, instance2).ShouldBeFalse();
    }
}