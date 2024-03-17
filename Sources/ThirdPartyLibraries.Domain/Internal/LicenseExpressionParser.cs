using System.Text.RegularExpressions;

namespace ThirdPartyLibraries.Domain.Internal;

// https://spdx.org/ids-how
internal static class LicenseExpressionParser
{
    private static readonly char[] Separators = { ' ', '(', ')' };
    private static readonly string[] Suffixes = { "+", "-only", "-or-later" };

    public static string[] Parse(string? text)
    {
        var expression = Trim(text);
        if (expression.Length == 0)
        {
            return Array.Empty<string>();
        }

        if (!IsExpression(expression))
        {
            expression = RemoveSuffix(expression);
            return expression.Length == 0 ? Array.Empty<string>() : new[] { expression };
        }

        var result = Split(expression);
        result.Sort(StringComparer.OrdinalIgnoreCase);
        
        return result.ToArray();
    }

    public static string ReplaceCodes(string text, string[] codes, Func<string, string> replacement)
    {
        var replacementByCode = new Dictionary<string, string>(codes.Length, StringComparer.OrdinalIgnoreCase);

        var pattern = new StringBuilder();
        foreach (var code in codes.OrderByDescending(i => i.Length))
        {
            replacementByCode.Add(code, replacement(code));

            if (pattern.Length > 0)
            {
                pattern.Append("|");
            }

            // (?<n1>code)
            pattern.Append("(").Append(Regex.Escape(code)).Append(")");
        }

        return Regex.Replace(text, pattern.ToString(), match => replacementByCode[match.Value], RegexOptions.IgnoreCase);
    }

    private static string Trim(string? text)
    {
        var result = text.AsSpan().Trim();
        if (result.IsEmpty)
        {
            return string.Empty;
        }

        return result.Length == text!.Length ? text : result.ToString();
    }

    private static List<string> Split(string text)
    {
        var words = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>(words.Length);
        
        var distinct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            if (IsOperator(word))
            {
                continue;
            }

            word = RemoveSuffix(word);
            if (word.Length > 0 && distinct.Add(word))
            {
                result.Add(word);
            }
        }

        return result;
    }

    private static bool IsOperator(string word)
    {
        return "AND".Equals(word, StringComparison.OrdinalIgnoreCase)
               || "OR".Equals(word, StringComparison.OrdinalIgnoreCase)
               || "WITH".Equals(word, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExpression(string text)
    {
        for (var i = 0; i < Separators.Length; i++)
        {
            if (text.Contains(Separators[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string RemoveSuffix(string code)
    {
        for (var i = 0; i < Suffixes.Length; i++)
        {
            var suffix = Suffixes[i];

            if (code.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return code.Substring(0, code.Length - suffix.Length);
            }
        }

        return code;
    }
}