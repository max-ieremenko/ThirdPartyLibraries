using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template
{
    public sealed class NuGetReadMeContext
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string LicenseCode { get; set; }

        public string LicenseLocalHRef { get; set; }

        public string LicenseDescription { get; set; }

        public string HRef { get; set; }

        public string Description { get; set; }

        public string UsedBy { get; set; }

        public IList<NuGetLibraryLicense> Licenses { get; } = new List<NuGetLibraryLicense>();

        public string TargetFrameworks { get; set; }

        public int DependenciesCount => Dependencies.Count;

        public IList<NuGetReadMeDependencyContext> Dependencies { get; } = new List<NuGetReadMeDependencyContext>();
        
        public string Remarks { get; set; }
    }
}
