using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Npm.Configuration;

namespace ThirdPartyLibraries.Npm.Internal;

internal sealed class NpmPackageLoader : IPackageLoader
{
    private readonly NpmPackageReference _reference;
    private readonly NpmConfiguration _configuration;
    private readonly INpmRegistry _registry;

    private byte[]? _packageContentCache;

    public NpmPackageLoader(NpmPackageReference reference, NpmConfiguration configuration, INpmRegistry registry)
    {
        _reference = reference;
        _configuration = configuration;
        _registry = registry;
    }

    public string RepositoryPackageFileName => NpmLibraryId.RepositoryPackageFileName;

    public string RepositorySpecFileName => NpmLibraryId.RepositoryPackageJsonFileName;

    public bool DownloadPackageIntoRepository => _configuration.DownloadPackageIntoRepository;

    public async Task<byte[]> DownloadPackageAsync(CancellationToken token)
    {
        if (_packageContentCache != null)
        {
            return _packageContentCache;
        }

        var content = await _registry.DownloadPackageAsync(_reference.Id.Name, _reference.Id.Version, token).ConfigureAwait(false);
        if (content == null)
        {
            throw new InvalidOperationException($"The npm package {_reference.Id.Name} {_reference.Id.Version} not found.");
        }

        _packageContentCache = content;
        return _packageContentCache;
    }

    public async Task<byte[]> GetSpecContentAsync(CancellationToken token)
    {
        var packageContent = await DownloadPackageAsync(token).ConfigureAwait(false);
        return NpmPackage.ExtractPackageJson(packageContent);
    }

    // TODO: not implemented
    public string? ResolvePackageSource() => null;

    public List<PackageSpecLicense> GetLicenses(Stream specContent)
    {
        var spec = NpmPackageSpec.FromStream(specContent);

        var result = new List<PackageSpecLicense>(3)
        {
            NpmSpecLicenseResolver.ResolvePackageLicense(spec.GetLicense())
        };

        var url = spec.GetRepositoryUrl();
        if (!string.IsNullOrEmpty(url))
        {
            result.Add(new PackageSpecLicense(PackageSpecLicenseType.Url, PackageSpecLicense.SubjectRepository, null, url));
        }

        url = spec.GetHomePage();
        if (!string.IsNullOrEmpty(url))
        {
            result.Add(new PackageSpecLicense(PackageSpecLicenseType.Url, PackageSpecLicense.SubjectHomePage, null, url));
        }

        return result;
    }

    public async Task<byte[]?> TryGetFileContentAsync(string fileName, CancellationToken token)
    {
        var packageContent = await DownloadPackageAsync(token).ConfigureAwait(false);
        return NpmPackage.LoadFileContent(packageContent, fileName);
    }

    public async Task<string[]> FindFilesAsync(string searchPattern, CancellationToken token)
    {
        var packageContent = await DownloadPackageAsync(token).ConfigureAwait(false);
        return NpmPackage.FindFiles(packageContent, searchPattern);
    }
}