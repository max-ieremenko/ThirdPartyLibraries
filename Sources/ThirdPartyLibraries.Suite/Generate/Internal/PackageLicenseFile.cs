using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal sealed class PackageLicenseFile
{
    public PackageLicenseFile(LibraryId id, string licenseCode, string fileName, ArrayHash hash)
    {
        Id = id;
        LicenseCode = licenseCode;
        FileName = fileName;
        Hash = hash;
    }

    public LibraryId Id { get; }

    public string LicenseCode { get; }

    public string FileName { get; }
    
    public ArrayHash Hash { get; }

    public string? RepositoryOwner { get; set; }

    public string? RepositoryName { get; set; }
}