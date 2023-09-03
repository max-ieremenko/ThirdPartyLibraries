using System;

namespace ThirdPartyLibraries.Npm.Configuration;

public sealed class NpmIgnoreFilterConfiguration
{
    public string[] ByName { get; set; } = Array.Empty<string>();

    public string[] ByFolderName { get; set; } = Array.Empty<string>();
}