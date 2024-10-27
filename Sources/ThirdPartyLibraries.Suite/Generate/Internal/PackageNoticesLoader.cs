using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal sealed class PackageNoticesLoader : IPackageNoticesLoader
{
    private readonly IStorage _storage;
    private readonly IPackageSpecLoader _specLoader;
    private readonly ILicenseHashBuilder _hashBuilder;
    private readonly IRepositoryNameParser[] _repositoryNameParsers;

    public PackageNoticesLoader(
        IStorage storage,
        IPackageSpecLoader specLoader,
        ILicenseHashBuilder hashBuilder,
        IEnumerable<IRepositoryNameParser> repositoryNameParsers)
    {
        _storage = storage;
        _specLoader = specLoader;
        _hashBuilder = hashBuilder;
        _repositoryNameParsers = repositoryNameParsers.ToArray();
    }

    public Task<PackageNotices?> LoadAsync(LibraryId id, List<string> appNames, CancellationToken token)
    {
        if (id.IsCustomSource())
        {
            return LoadCustomAsync(id, appNames, token);
        }

        return LoadOtherAsync(id, appNames, token);
    }

    private static bool FindLicense(List<LibraryLicense> licenses, string subject, [NotNullWhen(true)] out LibraryLicense? license)
    {
        for (var i = 0; i < licenses.Count; i++)
        {
            var candidate = licenses[i];
            if (subject.Equals(candidate.Subject, StringComparison.OrdinalIgnoreCase))
            {
                license = candidate;
                return true;
            }
        }

        license = null;
        return false;
    }

    private static bool UsePackage(List<string> appNames, IList<Application> usedBy)
    {
        for (var i = 0; i < appNames.Count; i++)
        {
            var appName = appNames[i];

            for (var j = 0; j < usedBy.Count; j++)
            {
                var app = usedBy[j];
                if (!app.InternalOnly && appName.Equals(app.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private async Task<PackageNotices?> LoadCustomAsync(LibraryId id, List<string> appNames, CancellationToken token)
    {
        var index = await _storage.ReadCustomLibraryIndexJsonAsync(id, token).ConfigureAwait(false);
        if (index == null || !UsePackage(appNames, index.UsedBy))
        {
            return null;
        }

        var result = new PackageNotices
        {
            Name = index.Name,
            Version = index.Version,
            LicenseCode = index.LicenseCode,
            Copyright = index.Copyright,
            Author = index.Author
        };

        if (Uri.TryCreate(index.HRef, UriKind.Absolute, out var href))
        {
            result.HRef = href;
        }

        result.ThirdPartyNotices = await _storage.ReadThirdPartyNoticesFileAsync(id, token).ConfigureAwait(false);

        return result;
    }

    private async Task<PackageNotices?> LoadOtherAsync(LibraryId id, List<string> appNames, CancellationToken token)
    {
        var index = await _storage.ReadLibraryIndexJsonAsync(id, token).ConfigureAwait(false);
        if (index == null || string.IsNullOrEmpty(index.License.Code) || !UsePackage(appNames, index.UsedBy))
        {
            return null;
        }

        var spec = await _specLoader.LoadAsync(id, token).ConfigureAwait(false);
        if (spec == null)
        {
            return null;
        }

        var result = new PackageNotices
        {
            Name = spec.GetName(),
            Version = spec.GetVersion(),
            LicenseCode = index.License.Code,
            Copyright = spec.GetCopyright(),
            Author = spec.GetAuthor(),
            HRef = _specLoader.ResolveParser(id).NormalizePackageSource(spec, index.Source).DownloadUrl
        };

        result.ThirdPartyNotices = await _storage.ReadThirdPartyNoticesFileAsync(id, token).ConfigureAwait(false);

        if (FindLicense(index.Licenses, PackageSpecLicense.SubjectPackage, out var packageLicense))
        {
            result.LicenseFile = await TryLoadFileAsync(id, index.License.Code, packageLicense, token).ConfigureAwait(false);
            if (Uri.TryCreate(packageLicense.HRef, UriKind.Absolute, out var url))
            {
                result.LicenseHRef = url;
            }
        }

        if (result.LicenseFile != null && result.LicenseHRef != null)
        {
            SetRepository(result.LicenseHRef, result.LicenseFile);
        }

        if (result.LicenseFile != null
            && string.IsNullOrEmpty(result.LicenseFile.RepositoryOwner)
            && FindLicense(index.Licenses, PackageSpecLicense.SubjectRepository, out var repositoryLicense)
            && Uri.TryCreate(repositoryLicense.HRef, UriKind.Absolute, out var repositoryUrl))
        {
            SetRepository(repositoryUrl, result.LicenseFile);
        }

        return result;
    }

    private async Task<PackageLicenseFile?> TryLoadFileAsync(LibraryId id, string licenseCode, LibraryLicense license, CancellationToken token)
    {
        var file = await _hashBuilder.GetHashAsync(id, license.Subject, token).ConfigureAwait(false);
        if (file.Hash == null)
        {
            return null;
        }

        return new PackageLicenseFile(id, licenseCode, file.FileName!, file.Hash.Value);
    }

    private void SetRepository(Uri url, PackageLicenseFile file)
    {
        for (var i = 0; i < _repositoryNameParsers.Length; i++)
        {
            if (_repositoryNameParsers[i].TryGetRepository(url, out var owner, out var name))
            {
                file.RepositoryOwner = owner;
                file.RepositoryName = name;
                return;
            }
        }
    }
}