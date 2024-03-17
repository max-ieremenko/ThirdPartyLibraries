using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Npm.Internal;

internal sealed class NpmPackageReference : IPackageReference
{
    public NpmPackageReference(LibraryId id, bool isInternal)
    {
        Id = id;
        Dependencies = new List<LibraryId>(0);
        IsInternal = isInternal;
    }

    public LibraryId Id { get; }

    public string[] TargetFrameworks => Array.Empty<string>();

    public List<LibraryId> Dependencies { get; }

    public bool IsInternal { get; }

    public IPackageReference UnionWith(IPackageReference other)
    {
        if (other is not NpmPackageReference || !other.Id.Equals(Id))
        {
            throw new ArgumentOutOfRangeException(nameof(other));
        }

        if (IsInternal == other.IsInternal)
        {
            return this;
        }

        return new NpmPackageReference(Id, IsInternal && other.IsInternal);
    }

    public override string ToString() => Id.ToString();
}