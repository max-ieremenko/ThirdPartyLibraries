namespace ThirdPartyLibraries.Repository.Template
{
    public sealed class RootReadMeLicenseContext
    {
        public string Code { get; set; }

        public string RequiresApproval { get; set; }

        public string RequiresThirdPartyNotices { get; set; }

        public string LocalHRef { get; set; }

        public int PackagesCount { get; set; }
    }
}