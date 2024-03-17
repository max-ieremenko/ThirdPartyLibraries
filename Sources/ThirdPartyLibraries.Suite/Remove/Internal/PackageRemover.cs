using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Remove.Internal;

internal sealed class PackageRemover : IPackageRemover
{
    private readonly IStorage _storage;

    public PackageRemover(IStorage storage)
    {
        _storage = storage;
    }

    public async Task<List<LibraryId>> GetAllLibrariesAsync(CancellationToken token)
    {
        var result = await _storage.GetAllLibrariesAsync(token).ConfigureAwait(false);

        // custom packages cannot be removed automatically
        result.RemoveAll(LibraryIdExtensions.IsCustomSource);

        return result;
    }

    public async Task<RemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token)
    {
        if (id.IsCustomSource())
        {
            throw new InvalidOperationException($"Library {id} is readonly.");
        }

        var index = await _storage.ReadLibraryIndexJsonAsync<LibraryIndexJson>(id, token).ConfigureAwait(false);
        if (index == null)
        {
            return RemoveResult.None;
        }

        var count = index.UsedBy.RemoveAll(i => appName.Equals(i.Name, StringComparison.OrdinalIgnoreCase));
        if (count == 0)
        {
            return RemoveResult.None;
        }

        if (index.UsedBy.Count == 0)
        {
            await _storage.RemoveLibraryAsync(id, token).ConfigureAwait(false);
            return RemoveResult.Deleted;
        }

        await _storage.WriteLibraryIndexJsonAsync(id, index, token).ConfigureAwait(false);
        return RemoveResult.Updated;
    }
}