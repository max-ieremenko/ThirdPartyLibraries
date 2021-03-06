﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.Suite.Internal
{
    [TestFixture]
    public class LicenseExpressionTest
    {
        // https://spdx.org/ids-how
        [Test]
        [TestCase("MIT", "MIT")]
        [TestCase("(MIT)", "MIT")]
        [TestCase("( MIT )", "MIT")]
        [TestCase("MIT OR Apache-2.0", "MIT", "Apache-2.0")]
        [TestCase("EPL-1.0+", "EPL-1.0")]
        [TestCase("GPL-2.0-only", "GPL-2.0")]
        [TestCase("GPL-2.0-or-later", "GPL-2.0")]
        [TestCase("GPL-3.0-only WITH Classpath-exception-2.0", "GPL-3.0", "Classpath-exception-2.0")]
        [TestCase("Apache-2.0 AND (MIT OR GPL-2.0-only)", "Apache-2.0", "MIT", "GPL-2.0")]
        public void GetCodes(string expression, params string[] expectedCodes)
        {
            LicenseExpression.GetCodes(expression).ShouldBe(expectedCodes);
        }

        [Test]
        [TestCase("MIT AND MIT+", "[MIT] AND [MIT]+")]
        [TestCase("MIT AND MIT2+", "[MIT] AND [MIT 2]+")]
        [TestCase("GPL-2.0-or-later", "[GPL-2.0]-or-later")]
        [TestCase("Apache-2.0 AND (MIT OR GPL-2.0-only)", "[Apache] AND ([MIT] OR [GPL-2.0]-only)")]
        public void ReplaceCodes(string expression, string expected)
        {
            var replacementByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Apache-2.0", "[Apache]" },
                { "MIT", "[MIT]" },
                { "MIT2", "[MIT 2]" },
                { "GPL-2.0", "[GPL-2.0]" }
            };

            LicenseExpression.ReplaceCodes(expression, i => replacementByCode[i]).ShouldBe(expected);
        }
    }
}
