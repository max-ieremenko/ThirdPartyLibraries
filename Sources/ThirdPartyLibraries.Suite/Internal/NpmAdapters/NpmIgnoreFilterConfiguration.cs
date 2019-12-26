using System;

namespace ThirdPartyLibraries.Suite.Internal.NpmAdapters
{
    internal sealed class NpmIgnoreFilterConfiguration
    {
        public string[] ByName { get; set; } = Array.Empty<string>();

        public string[] ByFolderName { get; set; } = Array.Empty<string>();
    }
}