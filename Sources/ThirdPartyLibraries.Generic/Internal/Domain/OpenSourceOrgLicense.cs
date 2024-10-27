namespace ThirdPartyLibraries.Generic.Internal.Domain;

internal sealed class OpenSourceOrgLicense
{
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }

    public OpenSourceOrgLicenseLink[]? Links { get; set; }

    public OpenSourceOrgLicenseText[]? Text { get; set; }

    public OpenSourceOrgLicenseIdentifier[]? Identifiers { get; set; }
}