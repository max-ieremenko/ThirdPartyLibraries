namespace ThirdPartyLibraries.Suite
{
    public interface IConfigurationManager
    {
        T GetSection<T>(string name)
            where T : new();
    }
}
