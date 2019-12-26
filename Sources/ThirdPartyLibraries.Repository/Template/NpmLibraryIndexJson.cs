using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template
{
    public sealed class NpmLibraryIndexJson
    {
        public LicenseConclusion License { get; } = new LicenseConclusion();

        public IList<NpmApplication> UsedBy { get; } = new List<NpmApplication>();

        public IList<LibraryLicense> Licenses { get; } = new List<LibraryLicense>();
    }
}