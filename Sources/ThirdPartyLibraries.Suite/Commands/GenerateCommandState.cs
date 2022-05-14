using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;
using ThirdPartyLibraries.Suite.Internal.NameCombiners;

namespace ThirdPartyLibraries.Suite.Commands;

internal sealed class GenerateCommandState
{
    private const string LicensesDirectory = "Licenses";

    private readonly Func<string, ILicenseSourceByUrl> _licenseSourceByHost;
    private readonly IDictionary<string, LicenseIndexJson> _indexByCode;
    private readonly IDictionary<string, ThirdPartyNoticesLicenseContext> _licenseByCode;
    private readonly IList<(Package Package, ThirdPartyNoticesPackageLicenseContext License)> _packages;
    private readonly LicenseFileGroups _licenseFileGroups;

    public GenerateCommandState(
        IPackageRepository repository,
        Func<string, ILicenseSourceByUrl> licenseSourceByHost,
        string to,
        ILogger logger)
    {
        _licenseSourceByHost = licenseSourceByHost;
        Repository = repository;
        To = to;
        Logger = logger;

        _indexByCode = new Dictionary<string, LicenseIndexJson>(StringComparer.OrdinalIgnoreCase);
        _licenseByCode = new Dictionary<string, ThirdPartyNoticesLicenseContext>(StringComparer.OrdinalIgnoreCase);
        _packages = new List<(Package Package, ThirdPartyNoticesPackageLicenseContext License)>();
        _licenseFileGroups = new LicenseFileGroups(repository);
    }

    public IPackageRepository Repository { get; }

    public string To { get; }

    public ILogger Logger { get; }

    public IEnumerable<ThirdPartyNoticesLicenseContext> Licenses => _licenseByCode.Values;

    public async Task<ThirdPartyNoticesLicenseContext> GetLicensesAsync(string licenseExpression, CancellationToken token)
    {
        if (_licenseByCode.TryGetValue(licenseExpression, out var result))
        {
            return result;
        }

        var codes = LicenseExpression.GetCodes(licenseExpression);
        result = new ThirdPartyNoticesLicenseContext { FullName = licenseExpression };

        foreach (var code in codes)
        {
            var index = await LoadLicenseIndexAsync(code, token).ConfigureAwait(false);
            if (index != null)
            {
                result.HRefs.Add(index.HRef);
                if (!index.FileName.IsNullOrEmpty())
                {
                    result.FileNames.Add(index.FileName);
                }

                if (codes.Count == 1)
                {
                    result.FullName = index.FullName;
                }
            }
        }

        _licenseByCode.Add(licenseExpression, result);
        return result;
    }

    public async Task<ThirdPartyNoticesPackageLicenseContext> GetPackageLicenseAsync(Package package, CancellationToken token)
    {
        var repositoryLicense = await GetLicensesAsync(package.LicenseCode, token).ConfigureAwait(false);

        var result = new ThirdPartyNoticesPackageLicenseContext
        {
            FullName = repositoryLicense.FullName
        };

        result.HRefs.AddRange(repositoryLicense.HRefs);

        if (package.TryFindLicense(PackageLicense.SubjectPackage, out var entryPackage))
        {
            Name alternativeName = null;
            if (!entryPackage.HRef.IsNullOrEmpty())
            {
                result.HRefs.Clear();
                result.HRefs.Add(entryPackage.HRef);
                alternativeName = GetAlternativeNameByHRef(entryPackage.HRef);
            }

            if (alternativeName == null
                && package.TryFindLicense(PackageLicense.SubjectRepository, out var entryRepository)
                && !entryRepository.HRef.IsNullOrEmpty())
            {
                alternativeName = GetAlternativeNameByHRef(entryRepository.HRef);
            }

            var libraryId = new LibraryId(package.SourceCode, package.Name, package.Version);
            await _licenseFileGroups.AddLicenseAsync(libraryId, Logger, package.LicenseCode, alternativeName, token).ConfigureAwait(false);
        }

        _packages.Add((package, result));
        return result;
    }

    public async Task AlignFileNamesAsync(CancellationToken token)
    {
        _licenseFileGroups.AlignFileNames();

        foreach (var (licenseExpression, license) in _licenseByCode)
        {
            var codes = LicenseExpression.GetCodes(licenseExpression);

            foreach (var code in codes)
            {
                var index = await LoadLicenseIndexAsync(code, token).ConfigureAwait(false);
                if (index != null && _licenseFileGroups.TryGetFileName(index, out var fileName))
                {
                    license.FileNames.Add(MapToLicensesDirectory(fileName));
                }
            }
        }

        foreach (var (package, license) in _packages)
        {
            var libraryId = new LibraryId(package.SourceCode, package.Name, package.Version);

            if (_licenseFileGroups.TryGetFileName(libraryId, out var fileName))
            {
                license.FileNames.Add(MapToLicensesDirectory(fileName));
            }
            else
            {
                var repositoryLicense = await GetLicensesAsync(package.LicenseCode, token).ConfigureAwait(false);
                license.FileNames.AddRange(repositoryLicense.FileNames);
            }
        }
    }

    public void CleanUpLicensesDirectory()
    {
        var path = Path.Combine(To, LicensesDirectory);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    public IEnumerable<string> GetAllLicenseFiles()
    {
        foreach (var fileName in _licenseFileGroups.GetAllFileNames())
        {
            yield return MapToLicensesDirectory(fileName);
        }
    }

    public Task CopyToLicensesDirectory(string fileName, CancellationToken token)
    {
        Directory.CreateDirectory(Path.Combine(To, LicensesDirectory));

        var destination = Path.Combine(To, fileName);
        var source = Path.GetFileName(fileName);
        return _licenseFileGroups.CopyFileAsync(source, destination, token);
    }

    private static string MapToLicensesDirectory(string fileName) => LicensesDirectory + "/" + fileName;

    private async Task<LicenseIndexJson> LoadLicenseIndexAsync(string code, CancellationToken token)
    {
        if (_indexByCode.TryGetValue(code, out var index))
        {
            return index;
        }

        index = await Repository.Storage.ReadLicenseIndexJsonAsync(code, token).ConfigureAwait(false);
        if (index == null)
        {
            Logger.Info("License {0} not found in the repository.".FormatWith(code));
            _indexByCode.Add(
                code,
                new LicenseIndexJson { Code = code });
            return null;
        }

        var result = new LicenseIndexJson
        {
            FullName = index.FullName.IsNullOrEmpty() ? index.Code : index.FullName,
            HRef = index.HRef,
            Code = index.Code
        };

        _indexByCode.Add(code, result);

        await _licenseFileGroups.AddLicenseAsync(index, Logger, token).ConfigureAwait(false);

        if (!index.Dependencies.IsNullOrEmpty())
        {
            foreach (var dependency in index.Dependencies)
            {
                await LoadLicenseIndexAsync(dependency, token).ConfigureAwait(false);
            }
        }

        return result;
    }

    private Name GetAlternativeNameByHRef(string href)
    {
        if (!Uri.TryCreate(href, UriKind.Absolute, out var url))
        {
            return null;
        }

        var licenseSourceByUrl = _licenseSourceByHost(url.Host);
        if (licenseSourceByUrl != null && licenseSourceByUrl.TryExtractRepositoryName(href, out var owner, out var name))
        {
            return new Name(owner, name);
        }

        return null;
    }
}