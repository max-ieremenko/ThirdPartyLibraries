using System.Security.Cryptography;

namespace ThirdPartyLibraries.Suite.Shared.Internal;

internal static class ArrayHashBuilder
{
    public static ArrayHash? FromStream(Stream? stream)
    {
        if (stream == null)
        {
            return null;
        }

        var hash = ComputeHash(stream);
        if (hash == null)
        {
            return null;
        }

        // 20 / 4
        var result = new int[hash.Length / sizeof(int)];
        for (var i = 0; i < result.Length; i++)
        {
            var startIndex = i * sizeof(int);
            result[i] = BitConverter.ToInt32(hash, startIndex);
        }

        return new ArrayHash(result);
    }

    private static byte[]? ComputeHash(Stream stream)
    {
        const int bufferSize = 1024;

        using (var sha = SHA1.Create())
        {
            using (var reader = new StreamReader(stream))
            using (var writer = new LicenseTextEncoder())
            {
                var text = new char[bufferSize];
                int length;
                while ((length = reader.Read(text, 0, bufferSize)) > 0)
                {
                    writer.Convert(text, length);

                    if (writer.BufferLength > 0)
                    {
                        var buffer = writer.GetBuffer();
                        sha.TransformBlock(buffer, 0, writer.BufferLength, null, 0);
                    }

                    writer.ClearBuffer();
                }

                if (writer.IsEmpty)
                {
                    return null;
                }
            }

            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return sha.Hash;
        }
    }
}