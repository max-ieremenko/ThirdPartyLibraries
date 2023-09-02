namespace ThirdPartyLibraries.Suite.Update.Internal;

internal sealed class IdenticalLicenseFile
{
    public IdenticalLicenseFile(string licenseCode, string description)
    {
        LicenseCode = licenseCode;
        Description = description;
    }

    public string LicenseCode { get; }
    
    public string Description { get; }
}