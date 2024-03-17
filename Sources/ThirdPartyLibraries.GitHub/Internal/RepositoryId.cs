namespace ThirdPartyLibraries.GitHub.Internal;

internal readonly struct RepositoryId : IEquatable<RepositoryId>
{
    public readonly string Owner;
    public readonly string Name;

    public RepositoryId(string owner, string name)
    {
        Owner = owner;
        Name = name;
    }

    public bool Equals(RepositoryId other)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(Owner, other.Owner)
               && StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name);
    }

    public override bool Equals(object obj) => obj is RepositoryId other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(Owner),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Name));
    }

    public override string ToString() => $"{Owner}/{Name}";
}