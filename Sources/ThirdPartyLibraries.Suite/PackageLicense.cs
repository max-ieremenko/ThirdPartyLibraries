namespace ThirdPartyLibraries.Suite
{
    public sealed class PackageLicense
    {
        public const string SubjectPackage = "package";
        public const string SubjectHomePage = "homepage";
        public const string SubjectRepository = "repository";
        public const string SubjectProject = "project";

        public static readonly string[] StaticLicenseFileNames = { "LICENSE.md", "LICENSE.txt", "LICENSE", "LICENSE.rtf" };

        public string Code { get; set; }

        public string CodeDescription { get; set; }

        public string Subject { get; set; }

        public string HRef { get; set; }

        public static string GetLicenseFileName(string subject, string fileName)
        {
            return subject + "-" + fileName;
        }
    }
}
