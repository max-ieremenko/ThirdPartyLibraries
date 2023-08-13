using System;
using System.Text;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal readonly struct ArrayHash : IEquatable<ArrayHash>, IComparable<ArrayHash>
{
    private readonly int[] _value;

    public ArrayHash(params int[] value)
    {
        _value = value;
    }

    public bool Equals(ArrayHash other)
    {
        var x = _value;
        var y = other._value;
        if (x.Length != y.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i])
            {
                return false;
            }
        }

        return true;
    }

    public int CompareTo(ArrayHash other)
    {
        var x = _value;
        var y = other._value;

        var c = x.Length.CompareTo(y.Length);
        if (c != 0)
        {
            return c;
        }

        for (var i = 0; i < x.Length; i++)
        {
            c = x[i].CompareTo(y[i]);
            if (c != 0)
            {
                return c;
            }
        }

        return 0;
    }

    public override bool Equals(object obj) => obj is ArrayHash other && Equals(other);

    public override int GetHashCode()
    {
        if (_value.Length == 0)
        {
            return 0;
        }

        var result = _value[0];
        for (var i = 1; i < _value.Length; i++)
        {
            result = HashCode.Combine(result, _value[i]);
        }

        return result;
    }

    public override string ToString() => string.Join(", ", _value);

    public void ToString(StringBuilder text, int bytesCount)
    {
        if (bytesCount < 0 || bytesCount > _value.Length * sizeof(int))
        {
            throw new ArgumentOutOfRangeException(nameof(bytesCount));
        }

        var valuesCount = bytesCount / sizeof(int);
        for (var i = 0; i < valuesCount; i++)
        {
            Write(text, i, sizeof(int));
        }

        var restCount = bytesCount % sizeof(int);
        if (restCount != 0)
        {
            Write(text, valuesCount, restCount);
        }
    }

    private void Write(StringBuilder text, int index, int count)
    {
        var bytes = BitConverter.GetBytes(_value[index]);
        for (var i = 0; i < count; i++)
        {
            text.Append(bytes[i].ToString("x2"));
        }
    }
}