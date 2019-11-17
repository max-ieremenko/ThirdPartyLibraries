using System;

namespace ThirdPartyLibraries.Suite
{
    public sealed class PackageNotices
    {
        // Newtonsoft.Json
        public string Name { get; set; }

        // 12.0.2
        public string Version { get; set; }

        public string LicenseCode { get; set; }

        // https://www.nuget.org/packages/Newtonsoft.Json/12.0.2
        public string HRef { get; set; }

        public string Author { get; set; }

        public string Copyright { get; set; }

        public string ThirdPartyNotices { get; set; }

        public PackageNoticesApplication[] UsedBy { get; set; } = Array.Empty<PackageNoticesApplication>();
    }
}
