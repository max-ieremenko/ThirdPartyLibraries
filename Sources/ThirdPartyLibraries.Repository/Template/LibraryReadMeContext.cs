namespace ThirdPartyLibraries.Repository.Template;

public sealed class LibraryReadMeContext
{
    public string Name { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string? Description { get; set; }

    public string HRef { get; set; } = null!;

    public string? LicenseCode { get; set; }

    public string? LicenseLocalHRef { get; set; }

    public string? LicenseMarkdownExpression { get; set; }

    public string? LicenseDescription { get; set; }

    public string UsedBy { get; set; } = null!;

    public IList<LibraryLicense> Licenses { get; } = new List<LibraryLicense>();

    public string? TargetFrameworks { get; set; }

    public int DependenciesCount => Dependencies.Count;

    public IList<LibraryReadMeDependencyContext> Dependencies { get; } = new List<LibraryReadMeDependencyContext>();
        
    public string? Remarks { get; set; }

    public string? ThirdPartyNotices { get; set; }
}