namespace ThirdPartyLibraries.Suite.Internal
{
    internal interface ILicenseCache
    {
        bool TryGetByUrl(string url, out LicenseInfo info);
        
        void AddByUrl(string url, LicenseInfo info);

        bool TryGetByCode(string code, out LicenseInfo info);
        
        void AddByCode(string code, LicenseInfo info);
    }
}
