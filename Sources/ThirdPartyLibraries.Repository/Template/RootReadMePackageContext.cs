using System.Diagnostics;

namespace ThirdPartyLibraries.Repository.Template
{
    [DebuggerDisplay("{Name}")]
    public sealed class RootReadMePackageContext
    {
        public string Source { get; set; }

        public string Name { get; set; }

        public string Version { get; set; }

        public string License { get; set; }

        public string LicenseLocalHRef { get; set; }

        public bool IsApproved { get; set; }

        public string ApprovalStatus { get; set; }

        public string LocalHRef { get; set; }

        public string SourceHRef { get; set; }

        public string UsedBy { get; set; }
    }
}