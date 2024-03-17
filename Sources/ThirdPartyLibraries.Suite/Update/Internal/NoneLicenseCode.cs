namespace ThirdPartyLibraries.Suite.Update.Internal;

internal static class NoneLicenseCode
{
    // https://spdx.dev/spdx-specification-21-web-version/
    public static bool IsNone(string? licenseCode)
    {
        return "NOASSERTION".Equals(licenseCode, StringComparison.OrdinalIgnoreCase)
               || "NONE".Equals(licenseCode, StringComparison.OrdinalIgnoreCase);
    }
}