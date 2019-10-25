using System.Collections.Generic;

namespace ThirdPartyLibraries.Generic
{
    public sealed class StaticLicenseConfiguration
    {
        public const string SectionName = "staticLicenseUrls";

        public IList<StaticLicenseByCode> ByCode { get; } = new List<StaticLicenseByCode>();

        public IList<StaticLicenseByUrl> ByUrl { get; } = new List<StaticLicenseByUrl>();
    }
}
