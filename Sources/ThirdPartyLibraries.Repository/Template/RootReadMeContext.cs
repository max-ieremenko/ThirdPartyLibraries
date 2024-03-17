namespace ThirdPartyLibraries.Repository.Template;

public sealed class RootReadMeContext
{
    public IList<RootReadMeLicenseContext> Licenses { get; } = new List<RootReadMeLicenseContext>();

    public int TodoPackagesCount => TodoPackages.Count;

    public IList<RootReadMePackageContext> TodoPackages { get; } = new List<RootReadMePackageContext>();

    public int PackagesCount => Packages.Count;

    public IList<RootReadMePackageContext> Packages { get; } = new List<RootReadMePackageContext>();
}