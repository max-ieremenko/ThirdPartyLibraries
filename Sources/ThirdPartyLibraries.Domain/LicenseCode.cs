using ThirdPartyLibraries.Domain.Internal;

namespace ThirdPartyLibraries.Domain;

public readonly struct LicenseCode : IEquatable<LicenseCode>
{
    public LicenseCode(string? text, string[] codes)
    {
        Text = text;
        Codes = codes;
    }

    public static LicenseCode Empty => new(null, Array.Empty<string>());

    public string? Text { get; }
    
    public string[] Codes { get; }

    public bool IsEmpty => Codes.Length == 0;

    public static bool IsSingleCode([NotNullWhen(true)] string? text)
    {
        return !string.IsNullOrEmpty(text) && !text.Contains(' ');
    }

    public static LicenseCode FromText(string? text)
    {
        var codes = LicenseExpressionParser.Parse(text);
        return new LicenseCode(text, codes);
    }

    public string? ReplaceCodes(Func<string, string> replacement)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return Text;
        }

        return LicenseExpressionParser.ReplaceCodes(Text, Codes, replacement);
    }

    public bool Equals(LicenseCode other)
    {
        if (Codes.Length != other.Codes.Length)
        {
            return false;
        }

        for (var i = 0; i < Codes.Length; i++)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(Codes[i], other.Codes[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is LicenseCode other && Equals(other);

    public override int GetHashCode()
    {
        if (Codes.Length == 0)
        {
            return 0;
        }

        var result = StringComparer.OrdinalIgnoreCase.GetHashCode(Codes[0]);
        for (var i = 1; i < Codes.Length; i++)
        {
            result = HashCode.Combine(result, StringComparer.OrdinalIgnoreCase.GetHashCode(Codes[i]));
        }

        return result;
    }

    public override string ToString() => Text ?? string.Empty;
}