using System.IO;
using Newtonsoft.Json;

namespace ThirdPartyLibraries
{
    public static class JsonSerializerExtensions
    {
        public static Stream JsonSerialize(this object content)
        {
            var result = new MemoryStream();

            using (var writer = new StreamWriter(result, leaveOpen: true))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                new JsonSerializer().Serialize(jsonWriter, content);
            }

            result.Position = 0;
            return result;
        }
    }
}
