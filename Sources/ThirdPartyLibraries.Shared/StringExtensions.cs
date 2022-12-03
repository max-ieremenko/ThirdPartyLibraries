using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ThirdPartyLibraries.Shared;

public static class StringExtensions
{
    [StringFormatMethod("format")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatWith(this string format, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, format, args);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty([CanBeNull] this string value)
    {
        return string.IsNullOrEmpty(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsIgnoreCase([CanBeNull] this string value, [CanBeNull] string other)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(value, other);
    }

    [ContractAnnotation("value:null => halt")]
    public static bool StartsWithIgnoreCase([CanBeNull] this string value, [CanBeNull] string other)
    {
        value.AssertNotNull(nameof(value));

        if (string.IsNullOrEmpty(other))
        {
            return true;
        }

        return value.StartsWith(other, StringComparison.OrdinalIgnoreCase);
    }

    [ContractAnnotation("value:null => halt")]
    public static bool EndsWithIgnoreCase([CanBeNull] this string value, [CanBeNull] string other)
    {
        value.AssertNotNull(nameof(value));

        if (string.IsNullOrEmpty(other))
        {
            return true;
        }

        return value.EndsWith(other, StringComparison.OrdinalIgnoreCase);
    }
}