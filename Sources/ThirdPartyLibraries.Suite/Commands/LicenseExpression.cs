using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Commands
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
