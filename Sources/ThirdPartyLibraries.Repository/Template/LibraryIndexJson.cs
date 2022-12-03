using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class LibraryIndexJson
{
    public LicenseConclusion License { get; } = new LicenseConclusion();

    public IList<Application> UsedBy { get; } = new List<Application>();

    public IList<LibraryLicense> Licenses { get; } = new List<LibraryLicense>();
}