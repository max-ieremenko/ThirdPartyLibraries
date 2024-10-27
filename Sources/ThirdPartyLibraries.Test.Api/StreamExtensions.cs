namespace ThirdPartyLibraries;

public static class StreamExtensions
{
    public static string AsText(this byte[] content)
    {
        using (var stream = new MemoryStream(content))
        using (var reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    public static byte[] AsBytes(this string content) => Encoding.UTF8.GetBytes(content);

    public static MemoryStream AsStream(this string content)
    {
        var data = AsBytes(content);
        return new MemoryStream(data, 0, data.Length, false, true);
    }
}