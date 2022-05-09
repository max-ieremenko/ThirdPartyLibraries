namespace ThirdPartyLibraries.Repository.Template;

public sealed class ThirdPartyNoticesPackageLicenseContext
{
    // license full name
    public string FullName { get; set; }

    // license public urls from the package spec, if not defined then repository license url
    public string HRef { get; set; }

    // license content from the package spec, if not defined then content from repository license file
    public string FileName { get; set; }
}