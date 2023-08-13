using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ThirdPartyLibraries.Shared;

public readonly struct IgnoreFilter
{
    public IgnoreFilter(IList<string>? patterns)
    {
        Patterns = patterns;
    }

    public IList<string>? Patterns { get; }

    public bool Filter(string name)
    {
        if (Patterns == null || Patterns.Count == 0)
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