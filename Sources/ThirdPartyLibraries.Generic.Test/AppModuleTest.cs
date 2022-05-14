using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic
{
    [TestFixture]
    public class AppModuleTest
    {
        private IServiceProvider _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<Func<HttpClient>>(() => new HttpClient());
            AppModule.ConfigureServices(services);

            _sut = services.BuildServiceProvider();
        }

        [Test]
        public void OpenSourceOrgApiIsSingleton()
        {
            var host1 = _sut.GetRequiredKeyedService<ILicenseCodeSource>(KnownHosts.OpenSourceOrg).ShouldBeOfType<OpenSourceOrgApi>();
            var host2 = _sut.GetRequiredKeyedService<ILicenseCodeSource>(KnownHosts.OpenSourceOrgApi).ShouldBeOfType<OpenSourceOrgApi>();

            ReferenceEquals(host1, host2).ShouldBeTrue();
        }

        [Test]
        public void ResolveCodeProjectApi()
        {
            _sut.GetRequiredKeyedService<ILicenseCodeSource>(KnownHosts.CodeProject).ShouldBeOfType<CodeProjectApi>();
            _sut.GetRequiredKeyedService<IFullLicenseSource>(CodeProjectApi.LicenseCode).ShouldBeOfType<CodeProjectApi>();
        }

        [Test]
        public void ResolveSpdxOrgApi()
        {
            _sut.GetRequiredKeyedService<ILicenseCodeSource>(KnownHosts.SpdxOrg.ToUpperInvariant()).ShouldBeOfType<SpdxOrgApi>();
            _sut.GetRequiredService<IFullLicenseSource>().ShouldBeOfType<SpdxOrgApi>();
        }
    }
}
