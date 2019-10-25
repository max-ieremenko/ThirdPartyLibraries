using System.Collections.Generic;
using System.Diagnostics;

namespace ThirdPartyLibraries.Suite
{
    [DebuggerDisplay("{Name} {Version}")]
    public sealed class Package
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

        public IList<PackageLicense> Licenses { get; } = new List<PackageLicense>();

        public IList<PackageAttachment> Attachments { get; } = new List<PackageAttachment>();
    }
}
