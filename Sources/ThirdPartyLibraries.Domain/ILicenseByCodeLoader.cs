namespace ThirdPartyLibraries.Domain;

public interface ILicenseByCodeLoader
{
    Task<LicenseSpec?> TryDownloadAsync(string code, CancellationToken token);
}