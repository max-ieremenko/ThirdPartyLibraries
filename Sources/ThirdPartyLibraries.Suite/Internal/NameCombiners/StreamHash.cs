using System;
using System.IO;
using System.Security.Cryptography;

namespace ThirdPartyLibraries.Suite.Internal.NameCombiners;

internal sealed class StreamHash : IEquatable<StreamHash>
{
    private int _hashCode;

    public StreamHash(byte[] value)
    {
        Value = value;
    }

    public static StreamHash Empty { get; } = FromStream(Stream.Null);

    public byte[] Value { get; }

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

        for (var i = 0; i < Value.Length; i++)
        {
            if (Value[i] != other.Value[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object obj) => Equals(obj as StreamHash);

    public override int GetHashCode()
    {
        if (_hashCode == 0)
        {
            int result = Value[0];
            for (var i = 1; i < Value.Length; i++)
            {
                result = HashCode.Combine(result, Value[i]);
            }

            _hashCode = result;
        }

        return _hashCode;
    }
}