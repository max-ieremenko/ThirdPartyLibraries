using System.IO;
using System.Text;

namespace ThirdPartyLibraries
{
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
    }
}
