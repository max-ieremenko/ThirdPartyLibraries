namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal interface ILicenseFileNameResolver
{
    void AddFile(LicenseFile file);

    void AddFile(PackageLicenseFile file);

    void Seal();

    FileSource ResolveFileSource(ArrayHash hash);
}