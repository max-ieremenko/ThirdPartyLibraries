namespace ThirdPartyLibraries.NuGet
{
    public sealed class NuGetSpec
    {
        public string Id { get; set; }

        public string Version { get; set; }

        public string PackageHRef { get; set; }

        public NuGetSpecLicense License { get; set; }

        public string LicenseUrl { get; set; }

        public string ProjectUrl { get; set; }

        public string Description { get; set; }

        public string Authors { get; set; }
        
        public string Copyright { get; set; }

        public NuGetSpecRepository Repository { get; set; }
    }
}
