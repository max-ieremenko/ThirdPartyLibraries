namespace ThirdPartyLibraries.Shared.DependencyInjection;

public partial class ServiceCollectionKeyedServiceExtensionsTest
{
    internal interface IService
    {
    }

    internal sealed class Service1 : IService
    {
        public string Foo { get; set; }
    }

    internal sealed class Service2 : IService
    {
    }
}