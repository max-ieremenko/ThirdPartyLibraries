namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal sealed class LicenseNotices
{
    public string Code { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public HashSet<Uri> HRefs { get; } = new();

    public HashSet<LicenseFile> Files { get; } = new();
}