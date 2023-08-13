namespace ThirdPartyLibraries.Suite.Shared;

internal sealed class PackageStorageLicenseFile
{
    public static string GetMask(string subject) => GetName(subject, "*lic*");

    public static string GetName(string subject, string fileName)
    {
        return subject + "-" + fileName;
    }
}