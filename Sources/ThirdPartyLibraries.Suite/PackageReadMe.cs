using System.Collections.Generic;
using System.Linq;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite
{
    public sealed class PackageReadMe
    {
        // nuget.org
        public string SourceCode { get; set; }

        // Newtonsoft.Json
        public string Name { get; set; }

        // 12.0.2
        public string Version { get; set; }

        // license conclusion
        public string LicenseCode { get; set; }

        public PackageApprovalStatus ApprovalStatus { get; set; }

        // https://www.nuget.org/packages/Newtonsoft.Json/12.0.2
        public string HRef { get; set; }

        public string UsedBy { get; set; }

        internal static string BuildUsedBy(IEnumerable<Application> applications)
        {
            applications.AssertNotNull(nameof(applications));

            return string.Join(", ", applications.Select(i => i.InternalOnly ? i.Name + " internal" : i.Name).OrderBy(i => i));
        }
    }
}
