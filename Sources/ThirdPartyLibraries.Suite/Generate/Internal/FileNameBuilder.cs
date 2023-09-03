using System.IO;
using System.Text;
using ThirdPartyLibraries.Suite.Shared;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal sealed class FileNameBuilder
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    private readonly ArrayHash _hash;

    private readonly StringBuilder _fileName;
    private readonly string? _extension;
    private int _nameLength;
    private string? _nameSuffix;
    private int _hashLength;

    public FileNameBuilder(string name, string? nameSuffix, string? extension, ArrayHash hash)
    {
        _fileName = new StringBuilder(EscapeInvalidChars(name));
        _nameLength = _fileName.Length;

        _nameSuffix = EscapeInvalidChars(nameSuffix);
        _extension = EscapeInvalidChars(extension)?.ToLowerInvariant();
        _hash = hash;

        _fileName.Append(_extension);
    }

    public void Expand()
    {
        if (!string.IsNullOrEmpty(_nameSuffix))
        {
            _fileName.Length = _nameLength;
            _fileName.Append('-').Append(_nameSuffix);
            _nameSuffix = null;
            _nameLength = _fileName.Length;

            _fileName.Append(_extension);
            return;
        }

        _fileName.Length = _nameLength;

        if (_hashLength == 0)
        {
            _hashLength = 3;
            if (!string.IsNullOrEmpty(_nameSuffix))
            {
                _fileName.Append(_extension);
                return;
            }
        }

        _fileName.Append('_');
        _hash.ToString(_fileName, _hashLength);

        _hashLength += 2;
        _fileName.Append(_extension);
    }

    public override string ToString() => _fileName.ToString();

    private static string? EscapeInvalidChars(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        char[]? result = null;
        for (var i = 0; i < text.Length; i++)
        {
            if (!IsInvalidChar(text[i]))
            {
                continue;
            }

            if (result == null)
            {
                result = text.ToCharArray();
            }

            result[i] = '_';
        }

        return result == null ? text : new string(result);
    }

    private static bool IsInvalidChar(char value)
    {
        for (var i = 0; i < InvalidFileNameChars.Length; i++)
        {
            if (InvalidFileNameChars[i] == value)
            {
                return true;
            }
        }

        return false;
    }
}