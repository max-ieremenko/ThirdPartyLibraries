﻿using System;
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

        // https://www.nuget.org/packages/Newtonsoft.Json/12.0.2
        public string HRef { get; set; }

        public string Author { get; set; }

        public string Copyright { get; set; }

        public string ThirdPartyNotices { get; set; }

        public string Remarks { get; set; }

        public string Description { get; set; }

        public IList<PackageLicense> Licenses { get; } = new List<PackageLicense>();

        public PackageApplication[] UsedBy { get; set; } = Array.Empty<PackageApplication>();
    }
}
