using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template
{
    public sealed class NpmReadMeContext
    {
        public string Name { get; set; }
        
        public string Version { get; set; }
        
        public string HRef { get; set; }
        
        public string LicenseCode { get; set; }
        
        public string UsedBy { get; set; }
        
        public string Description { get; set; }

        public string LicenseLocalHRef { get; set; }
        
        public string LicenseMarkdownExpression { get; set; }
        
        public string LicenseDescription { get; set; }

        public IList<LibraryLicense> Licenses { get; } = new List<LibraryLicense>();
        
        public string ThirdPartyNotices { get; set; }
        
        public string Remarks { get; set; }
    }
}
