using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal static class LicenseExpression
    {
        public static IList<string> GetCodes(string expression)
        {
            expression.AssertNotNull(nameof(expression));

            return expression
                .Split(new[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(i => !IsOperator(i))
                .Select(RemoveSuffix)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static string ReplaceCodes(string expression, Func<string, string> codeReplacement)
        {
            expression.AssertNotNull(nameof(expression));
            codeReplacement.AssertNotNull(nameof(codeReplacement));

            var codes = GetCodes(expression);
            var replacementByCode = new Dictionary<string, string>(codes.Count, StringComparer.OrdinalIgnoreCase);
            
            var pattern = new StringBuilder();
            foreach (var code in codes.OrderByDescending(i => i.Length))
            {
                replacementByCode.Add(code, codeReplacement(code));

                if (pattern.Length > 0)
                {
                    pattern.Append("|");
                }

                // (?<n1>code)
                pattern.Append("(").Append(Regex.Escape(code)).Append(")");
            }

            return Regex.Replace(expression, pattern.ToString(), match => replacementByCode[match.Value], RegexOptions.IgnoreCase);
        }

        private static bool IsOperator(string word)
        {
            return "AND".EqualsIgnoreCase(word)
                || "OR".EqualsIgnoreCase(word)
                || "WITH".EqualsIgnoreCase(word);
        }

        private static string RemoveSuffix(string code)
        {
            foreach (var suffix in new[] { "+", "-only", "-or-later" })
            {
                if (code.EndsWithIgnoreCase(suffix))
                {
                    return code.Substring(0, code.Length - suffix.Length);
                }
            }

            return code;
        }
    }
}
