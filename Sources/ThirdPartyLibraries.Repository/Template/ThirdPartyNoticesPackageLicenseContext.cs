namespace ThirdPartyLibraries.Repository.Template;

public sealed class ThirdPartyNoticesPackageLicenseContext
{
    // license full name
    public string FullName { get; set; } = null!;

    // license public url from the package spec, if not defined then repository license urls
    public List<string> HRefs { get; } = new();

    // license content from the package spec, if not defined then content from repository license files
    public List<string> FileNames { get; } = new();
}