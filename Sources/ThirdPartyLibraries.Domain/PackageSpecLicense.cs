namespace ThirdPartyLibraries.Domain;

public readonly struct PackageSpecLicense
{
    public const string SubjectPackage = "package";
    public const string SubjectHomePage = "homepage";
    public const string SubjectRepository = "repository";
    public const string SubjectProject = "project";

    public PackageSpecLicense(PackageSpecLicenseType type, string subject, string? code, string? href)
    {
        Type = type;
        Subject = subject;
        Code = code;
        Href = href;
    }

    public PackageSpecLicenseType Type { get; }

    public string Subject { get; }

    public string? Code { get; }

    public string? Href { get; }

    public override string ToString() => $"{Subject} {Type}/{Code}/{Href}";
}