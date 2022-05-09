using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;
using ThirdPartyLibraries.Suite.Internal.NameCombiners;

namespace ThirdPartyLibraries.Suite.Commands;

internal sealed class LicenseFileGroups
{
    private readonly IPackageRepository _repository;
    private readonly Dictionary<NamesGroupIndex, NamesGroup> _nameGroupByIndex;
    private readonly Dictionary<StreamHash, NamesGroup> _nameGroupByHash;
    private readonly NameCombiner _nameCombiner;
    private readonly IDictionary<string, NamesGroupIndex> _indexByFileName;
    private readonly Dictionary<NamesGroupIndex, string> _originalFileNameByIndex;

    public LicenseFileGroups(IPackageRepository repository)
    {
        _repository = repository;
        _nameGroupByIndex = new Dictionary<NamesGroupIndex, NamesGroup>();
        _nameGroupByHash = new Dictionary<StreamHash, NamesGroup>();
        _nameCombiner = new NameCombiner();
        _indexByFileName = new Dictionary<string, NamesGroupIndex>(StringComparer.OrdinalIgnoreCase);
        _originalFileNameByIndex = new Dictionary<NamesGroupIndex, string>();
    }

    public async Task AddLicenseAsync(LicenseIndexJson index, CancellationToken token)
    {
        if (index.FileName.IsNullOrEmpty())
        {
            return;
        }

        var group = new NamesGroup(
            index.Code,
            Path.GetExtension(index.FileName),
            "{0}-{1}".FormatWith(index.Code, Path.GetFileNameWithoutExtension(index.FileName)));

        var key = NamesGroupIndex.From(index);
        _nameGroupByIndex.Add(key, group);
        _originalFileNameByIndex.Add(key, index.FileName);

        using var stream = await _repository.Storage.OpenLicenseFileReadAsync(index.Code, index.FileName, token).ConfigureAwait(false);
        var hash = StreamHash.FromStream(stream);

        _nameGroupByHash.TryAdd(hash, group);
    }

    public async Task AddLicenseAsync(LibraryId libraryId, string licenseCode, Name alternativeName, CancellationToken token)
    {
        var fileNames = await _repository
            .Storage
            .FindLibraryFilesAsync(libraryId, PackageLicense.GetLicenseFileName(PackageLicense.SubjectPackage, "*lic*"), token)
            .ConfigureAwait(false);

        if (fileNames.Length == 0)
        {
            return;
        }

        using var stream = await _repository.Storage.OpenLibraryFileReadAsync(libraryId, fileNames[0], token).ConfigureAwait(false);
        var hash = StreamHash.FromStream(stream);

        if (!_nameGroupByHash.TryGetValue(hash, out var group))
        {
            group = new NamesGroup(licenseCode, Path.GetExtension(fileNames[0]), null);
            _nameGroupByHash.Add(hash, group);
        }

        group.Names.Add(new Name(libraryId.Name, libraryId.Version));

        if (alternativeName != null)
        {
            group.AlternativeNames.Add(alternativeName);
        }

        var key = NamesGroupIndex.From(libraryId);
        _nameGroupByIndex.Add(key, group);
        _originalFileNameByIndex.Add(key, fileNames[0]);
    }

    public void AlignFileNames()
    {
        _nameCombiner.Initialize(_nameGroupByHash.Values.Distinct());
    }

    public bool TryGetFileName(LicenseIndexJson index, out string fileName)
    {
        var key = NamesGroupIndex.From(index);

        fileName = null;
        if (!_nameGroupByIndex.TryGetValue(key, out var group))
        {
            return false;
        }

        fileName = _nameCombiner.GetFileName(group);
        StoreReference(key, fileName);
        return true;
    }

    public bool TryGetFileName(LibraryId libraryId, out string fileName)
    {
        var key = NamesGroupIndex.From(libraryId);

        fileName = null;
        if (!_nameGroupByIndex.TryGetValue(key, out var group))
        {
            return false;
        }

        fileName = _nameCombiner.GetFileName(group);
        StoreReference(key, fileName);
        return true;
    }

    public IEnumerable<string> GetAllFileNames() => _indexByFileName.Keys;

    public async Task CopyFileAsync(string fileName, string destination, CancellationToken token)
    {
        var index = _indexByFileName[fileName];
        var originalFileName = _originalFileNameByIndex[index];

        Stream sourceStream;
        if (index.AsLibraryId(out var libraryId))
        {
            sourceStream = await _repository.Storage.OpenLibraryFileReadAsync(libraryId, originalFileName, token).ConfigureAwait(false);
        }
        else
        {
            sourceStream = await _repository.Storage.OpenLicenseFileReadAsync(index.AsLicense(), originalFileName, token).ConfigureAwait(false);
        }

        using (sourceStream)
        using (var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.ReadWrite))
        {
            await sourceStream.CopyToAsync(destinationStream, token).ConfigureAwait(false);
        }
    }

    private void StoreReference(in NamesGroupIndex index, string fileName)
    {
        _indexByFileName.TryAdd(fileName, index);
    }

    private readonly struct NamesGroupIndex : IEquatable<NamesGroupIndex>
    {
        private readonly LibraryId _libraryId;

        private NamesGroupIndex(LibraryId libraryId)
        {
            _libraryId = libraryId;
        }

        public static NamesGroupIndex From(LicenseIndexJson index) => new(new LibraryId(index.Code, "-", "-"));

        public static NamesGroupIndex From(LibraryId libraryId) => new(libraryId);

        public bool Equals(NamesGroupIndex other) => _libraryId.Equals(other._libraryId);

        public override bool Equals(object obj)
        {
            return obj is NamesGroupIndex other && Equals(other);
        }

        public override int GetHashCode() => _libraryId.GetHashCode();

        public bool AsLibraryId(out LibraryId libraryId)
        {
            if (_libraryId.Name.EqualsIgnoreCase("-"))
            {
                libraryId = default;
                return false;
            }

            libraryId = _libraryId;
            return true;
        }

        public string AsLicense() => _libraryId.SourceCode;
    }
}