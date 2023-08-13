namespace ThirdPartyLibraries.Domain;

public enum LicenseSpecSource
{
    NotDefined,

    // like github repository license
    UserDefined,

    // repository/licenses, appsettings.json/staticLicenseUrls
    Configuration,

    // like spdx.org or opensource.org
    Shared
}