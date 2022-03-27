using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository
{
    public static class StorageFactory
    {
        public static IStorage Create(string connectionString)
        {
            connectionString.AssertNotNull(nameof(connectionString));

            return new FileStorage(FileTools.RootPath(connectionString));
        }
    }
}
