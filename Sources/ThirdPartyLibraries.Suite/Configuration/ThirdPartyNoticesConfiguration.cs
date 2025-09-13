namespace ThirdPartyLibraries.Suite.Configuration;

public sealed class ThirdPartyNoticesConfiguration
{
    public const string SectionName = "repository:third-party-notices.txt";

    private const string Always = "Always";
    private const string Never = "Never";
    private const string IfRequiredByLicense = "IfRequiredByLicense";

    public string KeepEmptyFile { get; set; } = Always;

    internal bool KeepEmptyFileAlways() => Always.Equals(KeepEmptyFile, StringComparison.OrdinalIgnoreCase);
 
    internal bool KeepEmptyFileIfLicense() => IfRequiredByLicense.Equals(KeepEmptyFile, StringComparison.OrdinalIgnoreCase);
    
    internal bool KeepEmptyFileNever() => Never.Equals(KeepEmptyFile, StringComparison.OrdinalIgnoreCase);
}