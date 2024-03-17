namespace ThirdPartyLibraries.Suite.Configuration;

public sealed class SkipCertificateCheckConfiguration
{
    public const string SectionName = "skipCertificateCheck";

    public string[] ByHost { get; set; } = Array.Empty<string>();

    public bool LogRequest { get; set; }
}