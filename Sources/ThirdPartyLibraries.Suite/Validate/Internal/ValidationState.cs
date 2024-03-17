using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Validate.Internal;

internal sealed class ValidationState : IValidationState
{
    private readonly IStorage _storage;
    private readonly HashSet<LibraryId> _notProcessed;
    private readonly Dictionary<LibraryId, ValidationResult> _resultById;

    public ValidationState(IStorage storage)
    {
        _storage = storage;
        _notProcessed = new HashSet<LibraryId>(0);
        _resultById = new Dictionary<LibraryId, ValidationResult>();
    }

    public async Task InitializeAsync(CancellationToken token)
    {
        var libraries = await _storage.GetAllLibrariesAsync(token).ConfigureAwait(false);
        _notProcessed.EnsureCapacity(libraries.Count);

        for (var i = 0; i < libraries.Count; i++)
        {
            var id = libraries[i];
            if (!id.IsCustomSource())
            {
                _notProcessed.Add(id);
            }
        }
    }

    public void SetResult(LibraryId id, ValidationResult result)
    {
        _notProcessed.Remove(id);

        if (result == ValidationResult.Success)
        {
            return;
        }

        if (_resultById.TryGetValue(id, out var existing))
        {
            _resultById[id] = result | existing;
        }
        else
        {
            _resultById.Add(id, result);
        }
    }

    public List<LibraryId> GetNotProcessed()
    {
        return new List<LibraryId>(_notProcessed);
    }

    public LibraryId[]? GetWithError(ValidationResult error)
    {
        var result = new List<LibraryId>();
        foreach (var entry in _resultById)
        {
            var flag = (entry.Value & error) == error;
            if (flag)
            {
                result.Add(entry.Key);
            }
        }

        if (result.Count == 0)
        {
            return null;
        }

        result.Sort();
        return result.ToArray();
    }
}