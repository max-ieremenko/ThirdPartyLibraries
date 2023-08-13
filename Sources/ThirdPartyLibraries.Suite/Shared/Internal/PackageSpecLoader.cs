using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Suite.Shared.Internal;

internal sealed class PackageSpecLoader : IPackageSpecLoader
{
    private readonly IStorage _storage;
    private readonly IPackageSpecParser[] _parsers;

    public PackageSpecLoader(IStorage storage, IEnumerable<IPackageSpecParser> parsers)
    {
        _storage = storage;
        _parsers = parsers.ToArray();
    }

    public IPackageSpecParser ResolveParser(LibraryId id)
    {
        for (var i = 0; i < _parsers.Length; i++)
        {
            var parser = _parsers[i];
            if (parser.CanParse(id))
            {
                return parser;
            }
        }

        throw new InvalidOperationException($"Package spec parser for {id.SourceCode} not found.");
    }

    public async Task<IPackageSpec?> LoadAsync(LibraryId id, CancellationToken token)
    {
        var parser = ResolveParser(id);

        var stream = await _storage.OpenLibraryFileReadAsync(id, parser.RepositorySpecFileName, token).ConfigureAwait(false);
        if (stream == null)
        {
            return null;
        }

        using (stream)
        {
            return parser.Parse(stream);
        }
    }
}