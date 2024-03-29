﻿using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.NuGet.Configuration;

namespace ThirdPartyLibraries.NuGet.Internal;

internal sealed class NuGetPackageLoader : IPackageLoader
{
    private readonly NuGetPackageReference _nuget;
    private readonly NuGetConfiguration _configuration;
    private readonly INuGetRepository _repository;

    private byte[]? _packageContentCache;

    public NuGetPackageLoader(NuGetPackageReference nuget, NuGetConfiguration configuration, INuGetRepository repository)
    {
        _nuget = nuget;
        _configuration = configuration;
        _repository = repository;
    }

    public string RepositoryPackageFileName => NuGetLibraryId.RepositoryPackageFileName;

    public string RepositorySpecFileName => NuGetLibraryId.RepositorySpecFileName;

    public bool DownloadPackageIntoRepository => _configuration.DownloadPackageIntoRepository;

    public async Task<byte[]> DownloadPackageAsync(CancellationToken token)
    {
        if (_packageContentCache != null)
        {
            return _packageContentCache;
        }

        if (_configuration.AllowToUseLocalCache)
        {
            // the fast way to get the content, skip caching
            var content = await _repository.TryGetPackageFromCacheAsync(_nuget.Id.Name, _nuget.Id.Version, _nuget.Sources, token).ConfigureAwait(false);
            if (content != null)
            {
                return content;
            }
        }

        if (_packageContentCache == null)
        {
            _packageContentCache = await _repository.TryDownloadPackageAsync(_nuget.Id.Name, _nuget.Id.Version, token).ConfigureAwait(false);
        }

        if (_packageContentCache == null)
        {
            throw new InvalidOperationException($"The nuget package {_nuget.Id.Name} {_nuget.Id.Version} not found.");
        }

        return _packageContentCache;
    }

    public async Task<byte[]> GetSpecContentAsync(CancellationToken token)
    {
        var packageContent = await DownloadPackageAsync(token).ConfigureAwait(false);
        var result = await NuGetPackage.ExtractSpecAsync(_nuget.Id.Name, packageContent, token).ConfigureAwait(false);
        return result;
    }

    public string? ResolvePackageSource()
    {
        return _repository.ResolvePackageSource(_nuget.Id.Name, _nuget.Id.Version, _nuget.Sources);
    }

    public List<PackageSpecLicense> GetLicenses(Stream specContent)
    {
        var spec = NuGetPackageSpec.FromStream(specContent);

        var result = new List<PackageSpecLicense>(3)
        {
            NuGetSpecLicenseResolver.ResolvePackageLicense(spec.GetLicenseType(), spec.GetLicenseValue(), spec.GetLicenseUrl())
        };

        var url = spec.GetRepositoryUrl();
        if (!string.IsNullOrEmpty(url))
        {
            result.Add(new PackageSpecLicense(PackageSpecLicenseType.Url, PackageSpecLicense.SubjectRepository, null, url));
        }

        url = spec.GetProjectUrl();
        if (!string.IsNullOrEmpty(spec.GetProjectUrl()))
        {
            result.Add(new PackageSpecLicense(PackageSpecLicenseType.Url, PackageSpecLicense.SubjectProject, null, url));
        }

        return result;
    }

    public async Task<byte[]?> TryGetFileContentAsync(string fileName, CancellationToken token)
    {
        var packageContent = await DownloadPackageAsync(token).ConfigureAwait(false);
        var result = await NuGetPackage.LoadFileContentAsync(packageContent, fileName, token).ConfigureAwait(false);
        return result;
    }

    public async Task<string[]> FindFilesAsync(string searchPattern, CancellationToken token)
    {
        var packageContent = await DownloadPackageAsync(token).ConfigureAwait(false);
        return NuGetPackage.FindFiles(packageContent, searchPattern);
    }
}