using System;

namespace ThirdPartyLibraries.Suite.Internal.NameCombiners;

internal sealed class Name : IEquatable<Name>, IComparable<Name>
{
    public Name(string first, string second)
    {
        First = first.ToLowerInvariant();
        Second = second.ToLowerInvariant();
    }

    public string First { get; }
    
    public string Second { get; }

    public bool Equals(Name other)
    {
        if (other == null)
        {
            return false;
        }

        return StringComparer.OrdinalIgnoreCase.Equals(First, other.First)
            && StringComparer.OrdinalIgnoreCase.Equals(Second, other.Second);
    }

    public int CompareTo(Name other)
    {
        if (other == null)
        {
            return 1;
        }

        var c = StringComparer.OrdinalIgnoreCase.Compare(First, other.First);
        if (c == 0)
        {
            c = StringComparer.OrdinalIgnoreCase.Compare(Second, other.Second);
        }

        return c;
    }

    public override bool Equals(object obj) => Equals(obj as Name);

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(First),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Second));
    }

    public override string ToString() => First + " - " + Second;
}