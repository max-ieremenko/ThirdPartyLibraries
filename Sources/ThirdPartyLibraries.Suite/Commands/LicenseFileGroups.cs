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

internal sealed class LicenseFileGroups
{
    private readonly IPackageRepository _repository;
    
    private readonly Dictionary<StreamHash, NamesGroup> _licenseGroupByHash;
    private readonly Dictionary<StreamHash, NamesGroup> _packageGroupByHash;

    private readonly Dictionary<NamesGroupIndex, StreamHash> _hashByIndex;

    private readonly NameCombiner _nameCombiner;
    private readonly IDictionary<string, NamesGroupIndex> _indexByFileName;
    private readonly Dictionary<NamesGroupIndex, string> _originalFileNameByIndex;

    public LicenseFileGroups(IPackageRepository repository)
    {
        _repository = repository;
        
        _licenseGroupByHash = new Dictionary<StreamHash, NamesGroup>();
        _packageGroupByHash = new Dictionary<StreamHash, NamesGroup>();

        _hashByIndex = new Dictionary<NamesGroupIndex, StreamHash>();

        _nameCombiner = new NameCombiner();
        _indexByFileName = new Dictionary<string, NamesGroupIndex>(StringComparer.OrdinalIgnoreCase);
        _originalFileNameByIndex = new Dictionary<NamesGroupIndex, string>();
    }

    public async Task AddLicenseAsync(LicenseIndexJson index, ILogger logger, CancellationToken token)
    {
        if (index.FileName.IsNullOrEmpty())
        {
            logger.Warn("{0} repository license does not contain license file.".FormatWith(index.Code));
            return;
        }

        using var stream = await _repository.Storage.OpenLicenseFileReadAsync(index.Code, index.FileName, token).ConfigureAwait(false);
        var hash = StreamHash.FromStream(stream);
        if (hash.Equals(StreamHash.Empty))
        {
            logger.Warn("{0} repository license file is empty.".FormatWith(index.Code));
            return;
        }

        if (_licenseGroupByHash.TryGetValue(hash, out var other))
        {
            logger.Warn("{0} repository license file duplicates {1} repository license file.".FormatWith(index.Code, other.GroupName));
            return;
        }

        var group = new NamesGroup(
            index.Code,
            hash.Value,
            Path.GetExtension(index.FileName),
            "{0}-{1}".FormatWith(index.Code, Path.GetFileNameWithoutExtension(index.FileName)));
        _licenseGroupByHash.Add(hash, group);

        var key = NamesGroupIndex.From(index);
        _hashByIndex.Add(key, hash);
        _originalFileNameByIndex.Add(key, index.FileName);
    }

    public async Task AddLicenseAsync(LibraryId libraryId, ILogger logger, string licenseCode, Name alternativeName, CancellationToken token)
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
        if (hash.Equals(StreamHash.Empty))
        {
            logger.Warn("Package {0} {1} from {2} contains empty license file.".FormatWith(libraryId.SourceCode, libraryId.Name, libraryId.Name));
            return;
        }

        if (!_packageGroupByHash.TryGetValue(hash, out var group))
        {
            group = new NamesGroup(licenseCode, hash.Value, Path.GetExtension(fileNames[0]), null);
            _packageGroupByHash.Add(hash, group);
        }

        group.Names.Add(new Name(libraryId.Name, libraryId.Version));

        if (alternativeName != null)
        {
            group.AlternativeNames.Add(alternativeName);
        }

        var key = NamesGroupIndex.From(libraryId);
        _hashByIndex.Add(key, hash);
        _originalFileNameByIndex.Add(key, fileNames[0]);
    }

    public void AlignFileNames()
    {
        foreach (var (hash, group) in _packageGroupByHash)
        {
            // try to attach package into repository licenses
            if (_licenseGroupByHash.TryGetValue(hash, out var licenseGroup))
            {
                licenseGroup.Names.AddRange(group.Names);
                licenseGroup.AlternativeNames.AddRange(group.AlternativeNames);
            }
            else
            {
                _licenseGroupByHash.Add(hash, group);
            }
        }

        _packageGroupByHash.Clear();

        _nameCombiner.Initialize(_licenseGroupByHash.Values);
    }

    public bool TryGetFileName(LicenseIndexJson index, out string fileName) => TryGetFileName(NamesGroupIndex.From(index), out fileName);

    public bool TryGetFileName(LibraryId libraryId, out string fileName) => TryGetFileName(NamesGroupIndex.From(libraryId), out fileName);

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

    private bool TryGetFileName(in NamesGroupIndex index, out string fileName)
    {
        if (!_hashByIndex.TryGetValue(index, out var hash))
        {
            fileName = null;
            return false;
        }

        var group = _licenseGroupByHash[hash];
        fileName = _nameCombiner.GetFileName(group);

        _indexByFileName.TryAdd(fileName, index);

        return true;
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