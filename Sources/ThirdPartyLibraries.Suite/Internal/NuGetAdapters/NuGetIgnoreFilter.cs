using System.Linq;
using System.Text.RegularExpressions;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.NuGetAdapters
{
    internal readonly struct NuGetIgnoreFilter
    {
        public NuGetIgnoreFilter(NuGetIgnoreFilterConfiguration configuration)
        {
            Configuration = configuration;
            configuration.AssertNotNull(nameof(configuration));
        }

        public NuGetIgnoreFilterConfiguration Configuration { get; }

        public bool FilterByName(string name) => Ignore(Configuration.ByName, name);

        public bool FilterByProjectName(string name) => Ignore(Configuration.ByProjectName, name);

        private static bool Ignore(string[] patterns, string name)
        {
            if (patterns.IsNullOrEmpty())
            {
                return false;
            }

            return patterns.Any(i => Regex.IsMatch(name, i, RegexOptions.IgnoreCase));
        }
    }
}
