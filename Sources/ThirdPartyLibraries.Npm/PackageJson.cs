namespace ThirdPartyLibraries.Npm
{
    public sealed class PackageJson
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string PackageHRef { get; set; }

        public string Description { get; set; }

        public string Authors { get; set; }

        public string HomePage { get; set; }

        public PackageJsonLicense License { get; set; }

        public PackageJsonRepository Repository { get; set; }
    }
}
