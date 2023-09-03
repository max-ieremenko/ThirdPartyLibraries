using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal sealed class PackageContentUpdater : IPackageContentUpdater
{
    private const string DefaultLicenseFilePattern = "LICENSE";

    private readonly IStorage _storage;
    private readonly ILicenseHashBuilder _hashBuilder;
    private readonly IPackageLoaderFactory[] _loaderFactories;

    public PackageContentUpdater(IStorage storage, ILicenseHashBuilder hashBuilder, IEnumerable<IPackageLoaderFactory> loaderFactories)
    {
        _storage = storage;
        _hashBuilder = hashBuilder;
        _loaderFactories = loaderFactories.ToArray();
    }

    public async Task<UpdateResult> UpdateAsync(IPackageReference reference, string appName, CancellationToken token)
    {
        if (reference.Id.IsCustomSource())
        {
            throw new ArgumentOutOfRangeException(nameof(reference));
        }

        var loader = ResolveLoader(reference);

        await EnsurePackageExistsAsync(reference.Id, loader, token).ConfigureAwait(false);
        await EnsureSpecExistsAsync(reference.Id, loader, token).ConfigureAwait(false);

        var index = await _storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(reference.Id, token).ConfigureAwait(false);
        if (index == null)
        {
            index = new LibraryIndexJson();
        }

        if (string.IsNullOrEmpty(index.Source))
        {
            index.Source = loader.ResolvePackageSource();
        }

        UpdateApp(index, appName, reference);
        await UpdateLicensesAsync(reference.Id, index, loader, token).ConfigureAwait(false);

        await _storage.CreateDefaultRemarksFileAsync(reference.Id, token).ConfigureAwait(false);
        await _storage.CreateDefaultThirdPartyNoticesFileAsync(reference.Id, token).ConfigureAwait(false);

        return await SaveLibraryIndexJsonAsync(reference.Id, index, token).ConfigureAwait(false);
    }

    private static void UpdateApp(LibraryIndexJson index, string appName, IPackageReference reference)
    {
        var app = index.UsedBy.FirstOrDefault(i => appName.Equals(i.Name, StringComparison.OrdinalIgnoreCase));
        if (app == null)
        {
            app = new Application { Name = appName };
            index.UsedBy.Add(app);
        }

        app.InternalOnly = reference.IsInternal;
        app.TargetFrameworks = reference.TargetFrameworks;
        app.Dependencies.Clear();
        foreach (var dependency in reference.Dependencies)
        {
            app.Dependencies.Add(new LibraryDependency { Name = dependency.Name, Version = dependency.Version });
        }
    }

    private async Task UpdateLicensesAsync(LibraryId id, LibraryIndexJson index, IPackageLoader loader, CancellationToken token)
    {
        var packageLicenses = await GetLicensesAsync(id, loader, token).ConfigureAwait(false);
        var toRemove = new List<LibraryLicense>(index.Licenses);

        for (var i = 0; i < packageLicenses.Count; i++)
        {
            var packageLicense = packageLicenses[i];
            var license = index.Licenses.Find(l => packageLicense.Subject.Equals(l.Subject, StringComparison.OrdinalIgnoreCase));

            if (license == null)
            {
                license = new LibraryLicense { Subject = packageLicense.Subject };
                index.Licenses.Add(license);
            }
            else
            {
                toRemove.Remove(license);
            }

            // do not override Code that may have been manually modified
            if (string.IsNullOrEmpty(license.Code))
            {
                license.Code = packageLicense.Code;
            }

            // do not override HRef that may have been manually modified
            if (string.IsNullOrEmpty(license.HRef))
            {
                license.HRef = packageLicense.Href;
            }

            await EnsureLicenseFileExistsAsync(id, license, packageLicense, loader, token).ConfigureAwait(false);
        }

        for (var i = 0; i < toRemove.Count; i++)
        {
            index.Licenses.Remove(toRemove[i]);
        }
    }

    private async Task EnsurePackageExistsAsync(LibraryId id, IPackageLoader loader, CancellationToken token)
    {
        var exists = await _storage.LibraryFileExistsAsync(id, loader.RepositoryPackageFileName, token).ConfigureAwait(false);

        if ((exists && loader.DownloadPackageIntoRepository)
            || (!exists && !loader.DownloadPackageIntoRepository))
        {
            return;
        }

        if (exists)
        {
            await _storage.RemoveLibraryFileAsync(id, loader.RepositoryPackageFileName, token).ConfigureAwait(false);
        }
        else
        {
            var content = await loader.DownloadPackageAsync(token).ConfigureAwait(false);
            await _storage.WriteLibraryFileAsync(id, loader.RepositoryPackageFileName, content, token).ConfigureAwait(false);
        }
    }

    private async Task EnsureSpecExistsAsync(LibraryId id, IPackageLoader loader, CancellationToken token)
    {
        var exists = await _storage.LibraryFileExistsAsync(id, loader.RepositorySpecFileName, token).ConfigureAwait(false);
        if (exists)
        {
            return;
        }

        var content = await loader.GetSpecContentAsync(token).ConfigureAwait(false);
        await _storage.WriteLibraryFileAsync(id, loader.RepositorySpecFileName, content, token).ConfigureAwait(false);
    }

    private async Task<List<PackageSpecLicense>> GetLicensesAsync(LibraryId id, IPackageLoader loader, CancellationToken token)
    {
        var specContent = await _storage.OpenLibraryFileReadAsync(id, loader.RepositorySpecFileName, token).ConfigureAwait(false);
        using (specContent)
        {
            return loader.GetLicenses(specContent!);
        }
    }

    private async Task<string?> TryFindWellKnowLicenseFileAsync(IPackageLoader loader, CancellationToken token)
    {
        var fileNames = await loader.FindFilesAsync(DefaultLicenseFilePattern, token).ConfigureAwait(false);
        if (fileNames.Length == 1)
        {
            return fileNames[0];
        }

        // LICENSE
        for (var i = 0; i < fileNames.Length; i++)
        {
            var fileName = fileNames[i];
            if (DefaultLicenseFilePattern.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }
        }

        // LICENSE.md, LICENSE.txt, LICENSE.rtf
        for (var i = 0; i < fileNames.Length; i++)
        {
            var fileName = fileNames[i];
            if (DefaultLicenseFilePattern.Equals(Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }
        }

        return null;
    }

    private async Task EnsureLicenseFileExistsAsync(
        LibraryId id,
        LibraryLicense license,
        PackageSpecLicense packageLicense,
        IPackageLoader loader,
        CancellationToken token)
    {
        var file = await _hashBuilder.GetHashAsync(id, license.Subject, token).ConfigureAwait(false);
        if (file.Hash != null)
        {
            return;
        }

        string? fileName = null;
        if (packageLicense.Type == PackageSpecLicenseType.File)
        {
            if (!string.IsNullOrEmpty(packageLicense.Href))
            {
                fileName = packageLicense.Href;
            }
        }
        else if (PackageSpecLicense.SubjectPackage.Equals(packageLicense.Subject, StringComparison.OrdinalIgnoreCase))
        {
            fileName = await TryFindWellKnowLicenseFileAsync(loader, token).ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        var content = await loader.TryGetFileContentAsync(fileName, token).ConfigureAwait(false);
        if (content != null)
        {
            var storageFileName = PackageStorageLicenseFile.GetName(packageLicense.Subject, fileName);
            await _storage.WriteLibraryFileAsync(id, storageFileName, content, token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(license.HRef))
            {
                license.HRef = fileName;
            }
        }
    }

    private async Task<UpdateResult> SaveLibraryIndexJsonAsync(LibraryId id, LibraryIndexJson index, CancellationToken token)
    {
        var original = await _storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(id, token).ConfigureAwait(false);

        var result = UpdateResult.None;
        if (LibraryIndexJsonChangeTracker.IsChanged(original, index))
        {
            result = original == null ? UpdateResult.Created : UpdateResult.Updated;
            LibraryIndexJsonChangeTracker.SortValues(index);
            await _storage.WriteLibraryIndexJsonAsync(id, index, token).ConfigureAwait(false);
        }

        return result;
    }

    private IPackageLoader ResolveLoader(IPackageReference reference)
    {
        for (var i = 0; i < _loaderFactories.Length; i++)
        {
            var loader = _loaderFactories[i].TryCreate(reference);
            if (loader != null)
            {
                return loader;
            }
        }

        throw new InvalidOperationException($"The package loader for [{reference.Id.SourceCode}] not found.");
    }
}