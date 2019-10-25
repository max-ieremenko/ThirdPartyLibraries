namespace ThirdPartyLibraries.Repository.Template
{
    public sealed class ThirdPartyNoticesPackageContext
    {
        public string Name { get; set; }

        public string HRef { get; set; }

        public string Author { get; set; }

        public string Copyright { get; set; }

        public ThirdPartyNoticesLicenseContext License { get; set; }
    }
}