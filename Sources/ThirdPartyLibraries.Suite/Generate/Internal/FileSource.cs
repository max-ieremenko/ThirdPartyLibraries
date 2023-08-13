using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal readonly struct FileSource
{
    public FileSource(string originalFileName, string reportFileName, string licenseCode)
    {
        OriginalFileName = originalFileName;
        ReportFileName = reportFileName;
        LicenseCode = licenseCode;
    }

    public FileSource(string originalFileName, string reportFileName, LibraryId library)
    {
        OriginalFileName = originalFileName;
        ReportFileName = reportFileName;
        Library = library;
    }

    public string OriginalFileName { get; }
    
    public string ReportFileName { get; }

    public LibraryId? Library { get; }

    public string? LicenseCode { get; }
}