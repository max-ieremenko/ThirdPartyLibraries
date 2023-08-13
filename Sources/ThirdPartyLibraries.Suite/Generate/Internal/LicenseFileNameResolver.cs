using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal sealed class LicenseFileNameResolver : ILicenseFileNameResolver
{
    private readonly Dictionary<string, HashSet<LicenseFile>> _licenseFilesByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PackageLicenseFile> _packageLicenseFiles = new();

    private readonly Dictionary<ArrayHash, FileSource> _fileSourceByHash = new();

    private bool _isSealed;

    public void AddFile(LicenseFile file)
    {
        Debug.Assert(!_isSealed, "!_isSealed");

        if (!_licenseFilesByCode.TryGetValue(file.LicenseCode, out var files))
        {
            files = new HashSet<LicenseFile>();
            _licenseFilesByCode.Add(file.LicenseCode, files);
        }

        files.Add(file);
    }

    public void AddFile(PackageLicenseFile file)
    {
        Debug.Assert(!_isSealed, "!_isSealed");

        _packageLicenseFiles.Add(file);
    }

    public void Seal()
    {
        Debug.Assert(!_isSealed, "!_isSealed");
        _isSealed = true;

        ProcessLicenseFiles();
        ProcessPackages();

        _licenseFilesByCode.Clear();
        _packageLicenseFiles.Clear();
    }

    public FileSource ResolveFileSource(ArrayHash hash)
    {
        Debug.Assert(_isSealed, "_isSealed");

        return _fileSourceByHash[hash];
    }

    private void ProcessLicenseFiles()
    {
        var names = new List<(FileNameBuilder, LicenseFile)>();

        foreach (var files in _licenseFilesByCode.Values)
        {
            foreach (var file in files)
            {
                var name = new FileNameBuilder(file.LicenseCode + "-license", null, Path.GetExtension(file.FileName), file.Hash);
                names.Add((name, file));
            }
        }

        names.MakeUnique();
        for (var i = 0; i < names.Count; i++)
        {
            var (name, file) = names[i];
            AddSource(file, name.ToString());
        }
    }

    private void ProcessPackages()
    {
        var groups = _packageLicenseFiles.GroupBy(i => i.Hash);
        var names = new List<(FileNameBuilder, PackageLicenseFile)>();

        foreach (var group in groups)
        {
            if (_fileSourceByHash.ContainsKey(group.Key))
            {
                // from repository license
                continue;
            }

            var files = group.ToArray();
            var ext = files.GetExtension();
            var (name, suffix) = GetPackageNames(files);

            names.Add((new FileNameBuilder(name, suffix, ext, files[0].Hash), files[0]));
        }

        names.MakeUnique();
        for (var i = 0; i < names.Count; i++)
        {
            var (name, file) = names[i];
            AddSource(file, name.ToString());
        }
    }

    private (string Name, string? Suffix) GetPackageNames(PackageLicenseFile[] files)
    {
        var packageName = files.TryFindCommonPackageName();
        if (packageName != null)
        {
            var version = files.TryFindCommonPackageVersion();
            return (packageName, version);
        }

        var repositoryName = files.TryFindCommonRepositoryName();
        var repositoryOwner = files.TryFindCommonRepositoryOwner();
        if (repositoryOwner != null)
        {
            return (repositoryOwner, repositoryName);
        }

        if (repositoryName != null)
        {
            return (repositoryName, null);
        }

        return (NormalizeLicenseCode(files[0].LicenseCode), null);
    }

    private void AddSource(LicenseFile file, string reportFileName)
    {
        if (!_fileSourceByHash.ContainsKey(file.Hash))
        {
            var source = new FileSource(file.FileName, reportFileName, file.LicenseCode);
            _fileSourceByHash.Add(file.Hash, source);
        }
    }

    private void AddSource(PackageLicenseFile file, string reportFileName)
    {
        if (!_fileSourceByHash.ContainsKey(file.Hash))
        {
            var source = new FileSource(file.FileName, reportFileName, file.Id);
            _fileSourceByHash.Add(file.Hash, source);
        }
    }

    private string NormalizeLicenseCode(string code)
    {
        if (_licenseFilesByCode.TryGetValue(code, out var files))
        {
            return files.First().LicenseCode;
        }

        return code;
    }
}