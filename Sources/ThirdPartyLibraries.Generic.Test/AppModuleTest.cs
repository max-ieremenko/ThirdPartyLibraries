using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Generic
{
    [TestFixture]
    public class AppModuleTest
    {
        private IUnityContainer _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            _sut = new UnityContainer();
            AppModule.ConfigureContainer(_sut);
        }

        [Test]
        public void OpenSourceOrgApiIsSingleton()
        {
            var host1 = _sut.Resolve<ILicenseCodeSource>(KnownHosts.OpenSourceOrg).ShouldBeOfType<OpenSourceOrgApi>();
            var host2 = _sut.Resolve<ILicenseCodeSource>(KnownHosts.OpenSourceOrgApi).ShouldBeOfType<OpenSourceOrgApi>();

            ReferenceEquals(host1, host2).ShouldBeTrue();
        }
    }
}
