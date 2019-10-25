using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template
{
    public sealed class ThirdPartyNoticesContext
    {
        public IList<ThirdPartyNoticesLicenseContext> Licenses { get; } = new List<ThirdPartyNoticesLicenseContext>();

        public IList<ThirdPartyNoticesPackageContext> Packages { get; } = new List<ThirdPartyNoticesPackageContext>();
    }
}
