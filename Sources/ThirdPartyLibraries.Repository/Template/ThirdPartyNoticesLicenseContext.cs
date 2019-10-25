using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template
{
    public sealed class ThirdPartyNoticesLicenseContext
    {
        public string FullName { get; set; }

        public IList<string> HRefs { get; } = new List<string>();

        public IList<string> FileNames { get; } = new List<string>();

        public IList<ThirdPartyNoticesPackageContext> Packages { get; } = new List<ThirdPartyNoticesPackageContext>();
    }
}