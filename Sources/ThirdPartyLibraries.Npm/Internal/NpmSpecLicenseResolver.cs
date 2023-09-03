using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Npm.Internal;

internal static class NpmSpecLicenseResolver
{
    public static PackageSpecLicense ResolvePackageLicense((PackageSpecLicenseType Type, string? Value) source)
    {
        string? code = null;
        string? href = null;
        if (source.Type == PackageSpecLicenseType.File)
        {
            href = source.Value;
        }
        else if (source.Type == PackageSpecLicenseType.Expression)
        {
            code = source.Value;
        }

        return new PackageSpecLicense(source.Type, PackageSpecLicense.SubjectPackage, code, href);
    }
}