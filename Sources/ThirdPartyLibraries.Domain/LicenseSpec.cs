namespace ThirdPartyLibraries.Domain;

public sealed class LicenseSpec
{
    public LicenseSpec(LicenseSpecSource source, string code)
    {
        Source = source;
        Code = code;
    }

    public LicenseSpecSource Source { get; }

    public string Code { get; }

    public string? FullName { get; set; }

    public string? FileName { get; set; }

    public string? FileExtension { get; set; }

    public byte[]? FileContent { get; set; }

    public string? HRef { get; set; }
}