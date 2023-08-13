using System;
using System.IO;
using System.Text;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal sealed partial class LicenseTextEncoder : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly StreamWriter _writer;
    private LastChar _lastChar;

    public LicenseTextEncoder()
    {
        _stream = new MemoryStream();
        _writer = new StreamWriter(_stream, Encoding.UTF8);
        IsEmpty = true;
    }

    public bool IsEmpty { get; private set; }

    public int BufferLength => (int)_stream.Length;

    public void Convert(char[] text, int count)
    {
        Write(text, count);
        _writer.Flush();
    }

    public void ClearBuffer() => _stream.SetLength(0);

    public byte[] GetBuffer() => _stream.GetBuffer();

    public void Dispose()
    {
        _writer.Dispose();
        _stream.Dispose();
    }

    private void Write(char[] text, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var value = text[i];

            if (char.IsWhiteSpace(value))
            {
                WriteSpace();
            }
            else if (char.IsLetterOrDigit(value))
            {
                WriteLetter(value);
                IsEmpty = false;
            }
            else
            {
                WriteOther(value);
                IsEmpty = false;
            }
        }
    }

    private void WriteOther(char value)
    {
        _writer.Write(value);
        _lastChar = LastChar.Other;
    }

    private void WriteLetter(char value)
    {
        if (_lastChar == LastChar.MissingSpace)
        {
            _writer.Write(' ');
        }

        _lastChar = LastChar.Letter;
        _writer.Write(value);
    }

    private void WriteSpace()
    {
        if (_lastChar != LastChar.Other && _lastChar != LastChar.None)
        {
            _lastChar = LastChar.MissingSpace;
        }
    }
}