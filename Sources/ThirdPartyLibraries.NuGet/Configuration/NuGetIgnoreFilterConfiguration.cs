using System;

namespace ThirdPartyLibraries.NuGet.Configuration;

public sealed class NuGetIgnoreFilterConfiguration
{
    public string[] ByName { get; set; } = Array.Empty<string>();

    public string[] ByProjectName { get; set; } = Array.Empty<string>();
}