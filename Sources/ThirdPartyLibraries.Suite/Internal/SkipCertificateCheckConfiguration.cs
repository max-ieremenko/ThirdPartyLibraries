using System;

namespace ThirdPartyLibraries.Suite.Internal;

public sealed class SkipCertificateCheckConfiguration
{
    public const string SectionName = "skipCertificateCheck";

    public string[] ByHost { get; set; } = Array.Empty<string>();
}