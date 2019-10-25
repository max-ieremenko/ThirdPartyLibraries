namespace ThirdPartyLibraries.GitHub
{
    public struct GitHubLicense
    {
        public string SpdxId { get; set; }

        public string SpdxIdHRef { get; set; }

        public string FileName { get; set; }

        public byte[] FileContent { get; set; }

        public string FileContentHRef { get; set; }
    }
}