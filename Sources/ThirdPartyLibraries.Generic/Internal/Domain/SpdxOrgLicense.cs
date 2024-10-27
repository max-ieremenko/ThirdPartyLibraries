namespace ThirdPartyLibraries.Generic.Internal.Domain;

internal sealed class SpdxOrgLicense
{
    public string? LicenseId { get; set; }

    public string? Name { get; set; }

    public string LicenseText { get; set; } = string.Empty;
}