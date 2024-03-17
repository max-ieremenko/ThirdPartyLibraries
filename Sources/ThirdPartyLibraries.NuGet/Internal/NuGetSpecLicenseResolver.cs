using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.NuGet.Internal;

internal static class NuGetSpecLicenseResolver
{
    public static PackageSpecLicense ResolvePackageLicense(
        string? licenseType,
        string? licenseValue,
        string? licenseUrl)
    {
        // licenseUrl is deprecated. Use license instead.
        var specLicenseUrl = IsDeprecateLicenseUrl(licenseUrl) ? null : licenseUrl;

        PackageSpecLicenseType specLicenseType;
        string? href;
        string? code;
        if ("file".Equals(licenseType, StringComparison.OrdinalIgnoreCase))
        {
            specLicenseType = PackageSpecLicenseType.File;
            code = null;
            href = licenseValue;
        }
        else if ("expression".Equals(licenseType, StringComparison.OrdinalIgnoreCase))
        {
            specLicenseType = PackageSpecLicenseType.Expression;
            code = licenseValue;
            href = specLicenseUrl;
        }
        else if (!string.IsNullOrEmpty(specLicenseUrl))
        {
            specLicenseType = PackageSpecLicenseType.Url;
            code = null;
            href = specLicenseUrl;
        }
        else
        {
            specLicenseType = PackageSpecLicenseType.NotDefined;
            code = null;
            href = null;
        }

        return new PackageSpecLicense(specLicenseType, PackageSpecLicense.SubjectPackage, code, href);
    }

    internal static bool IsDeprecateLicenseUrl(string? value)
    {
        // https://aka.ms/deprecateLicenseUrl
        if (string.IsNullOrEmpty(value) || !Uri.TryCreate(value, UriKind.Absolute, out var url))
        {
            return false;
        }

        return url.Host.Equals("aka.ms", StringComparison.OrdinalIgnoreCase)
               && url.LocalPath.StartsWith("/deprecateLicenseUrl", StringComparison.OrdinalIgnoreCase);
    }
}