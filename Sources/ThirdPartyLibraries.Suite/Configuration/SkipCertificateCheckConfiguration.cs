using System;
using System.Diagnostics;

namespace ThirdPartyLibraries.Suite.Configuration;

public sealed class SkipCertificateCheckConfiguration
{
    public const string SectionName = "skipCertificateCheck";

    public SkipCertificateCheckConfiguration()
    {
        ForceLogRequest();
    }

    public string[] ByHost { get; set; } = Array.Empty<string>();

    public bool LogRequest { get; set; }

    [Conditional("DEBUG")]
    private void ForceLogRequest()
    {
        LogRequest = true;
    }
}