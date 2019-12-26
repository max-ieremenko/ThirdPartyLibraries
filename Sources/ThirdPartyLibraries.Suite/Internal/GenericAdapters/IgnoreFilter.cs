using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.GenericAdapters
{
    internal readonly struct IgnoreFilter
    {
        public IgnoreFilter(IList<string> patterns)
        {
            Patterns = patterns;
        }

        public IList<string> Patterns { get; }

        public bool Filter(string name)
        {
            if (Patterns.IsNullOrEmpty())
            {
                return false;
            }

            return Patterns.Any(i => Regex.IsMatch(name, i, RegexOptions.IgnoreCase));
        }
    }
}
