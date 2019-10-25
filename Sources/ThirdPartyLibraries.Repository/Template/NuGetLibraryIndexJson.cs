using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template
{
    public sealed class NuGetLibraryIndexJson
    {
        public LicenseConclusion License { get; } = new LicenseConclusion();

        public IList<Application> UsedBy { get; } = new List<Application>();

        public IList<NuGetLibraryLicense> Licenses { get; } = new List<NuGetLibraryLicense>();
    }
}
