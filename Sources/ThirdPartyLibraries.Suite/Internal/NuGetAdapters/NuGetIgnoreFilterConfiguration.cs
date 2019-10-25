using System;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal sealed class NuGetIgnoreFilterConfiguration
    {
        public string[] ByName { get; set; } = Array.Empty<string>();

        public string[] ByProjectName { get; set; } = Array.Empty<string>();
    }
}
