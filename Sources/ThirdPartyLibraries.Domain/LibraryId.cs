namespace ThirdPartyLibraries.Domain;

public readonly struct LibraryId : IEquatable<LibraryId>, IComparable<LibraryId>
{
    public LibraryId(string sourceCode, string name, string version)
    {
        SourceCode = sourceCode;
        Name = name;
        Version = version;
    }

    public string SourceCode { get; }

    public string Name { get; }

    public string Version { get; }

    public bool Equals(LibraryId other)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(SourceCode, other.SourceCode)
               && StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name)
               && StringComparer.OrdinalIgnoreCase.Equals(Version, other.Version);
    }

    public int CompareTo(LibraryId other)
    {
        var c = StringComparer.OrdinalIgnoreCase.Compare(Name, other.Name);
        if (c == 0)
        {
            c = StringComparer.OrdinalIgnoreCase.Compare(Version, other.Version);
        }

        if (c == 0)
        {
            c = StringComparer.OrdinalIgnoreCase.Compare(SourceCode, other.SourceCode);
        }

        return c;
    }

    public override bool Equals(object? obj) => obj is LibraryId other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(SourceCode),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Name),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Version));
    }

    public override string ToString() => $"{SourceCode}/{Name}/{Version}";
}