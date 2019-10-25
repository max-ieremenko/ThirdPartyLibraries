namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal sealed class NuGetConfiguration
    {
        public NuGetIgnoreFilterConfiguration IgnorePackages { get; set; } = new NuGetIgnoreFilterConfiguration();

        public NuGetIgnoreFilterConfiguration InternalPackages { get; set; } = new NuGetIgnoreFilterConfiguration();

        public bool AllowToUseLocalCache { get; set; }
    }
}
