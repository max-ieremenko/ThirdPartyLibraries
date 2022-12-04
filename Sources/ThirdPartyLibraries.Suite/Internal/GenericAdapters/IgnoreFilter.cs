using System.Collections.Generic;
using System.Text.RegularExpressions;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal.GenericAdapters;

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

        for (var i = 0; i < Patterns.Count; i++)
        {
            var pattern = Patterns[i];
            if (Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}