namespace ThirdPartyLibraries.Suite.Validate.Internal;

[Flags]
internal enum ValidationResult
{
    Success,

    IndexNotFound = 1,

    NotAssignedToIndex = 2,

    NoLicenseCode = 4,

    LicenseNotFound = 8,

    LicenseNotApproved = 16,

    NoThirdPartyNotices = 32,

    ReferenceNotFound = 64
}