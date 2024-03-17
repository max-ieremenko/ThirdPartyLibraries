namespace ThirdPartyLibraries.Domain;

public interface ILicenseByUrlLoader
{
    Task<LicenseSpec?> TryDownloadAsync(Uri url, CancellationToken token);
}