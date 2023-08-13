using System.Collections.Generic;

namespace ThirdPartyLibraries.Generic.Configuration;

public sealed class StaticLicenseConfiguration
{
    public const string SectionName = "staticLicenseUrls";

    public List<StaticLicenseByCode> ByCode { get; } = new();

    public List<StaticLicenseByUrl> ByUrl { get; } = new();
}