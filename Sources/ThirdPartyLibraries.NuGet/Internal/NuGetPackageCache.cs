using System.Runtime.InteropServices;

namespace ThirdPartyLibraries.NuGet.Internal;

internal readonly struct NuGetPackageCache
{
    private readonly string _packageName;
    private readonly string _version;

    public NuGetPackageCache(string packageName, string version)
    {
        _packageName = packageName.ToLowerInvariant();
        _version = version.ToLowerInvariant();
    }

    public string? GetDefaultCachePath()
    {
        string? userProfile;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        }
        else
        {
            userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (string.IsNullOrEmpty(userProfile) || !Directory.Exists(userProfile))
        {
            return null;
        }

        return Path.Combine(userProfile, ".nuget", "packages");
    }

    public string GetPackageFileName() => $"{_packageName}.{_version}.nupkg";

    public string GetMetadataFileName() => ".nupkg.metadata";

    public bool TryFindFile(string? basePath, string fileName, [NotNullWhen(true)] out string? path)
    {
        path = null;
        if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
        {
            return false;
        }

        var result = Path.Combine(basePath, _packageName, _version, fileName);
        if (File.Exists(result))
        {
            path = result;
            return true;
        }

        return false;
    }
}