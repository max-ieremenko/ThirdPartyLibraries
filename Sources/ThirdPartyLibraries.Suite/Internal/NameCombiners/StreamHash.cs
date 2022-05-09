using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ThirdPartyLibraries.Suite.Internal.NameCombiners;

internal sealed class StreamHash : IEquatable<StreamHash>
{
    private readonly byte[] _value;

    public StreamHash(byte[] value)
    {
        _value = value;
    }

    public static StreamHash FromStream(Stream stream)
    {
        using var sha = SHA1.Create();
        
        var hash = sha.ComputeHash(stream);

        return new StreamHash(hash);
    }

    public bool Equals(StreamHash other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _value.SequenceEqual(other._value);
    }

    public override bool Equals(object obj) => Equals(obj as StreamHash);

    public override int GetHashCode()
    {
        int result = _value[0];
        for (var i = 1; i < _value.Length; i++)
        {
            result = HashCode.Combine(result, _value[i]);
        }

        return result;
    }
}