using System.IO;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet
{
    public static class NuGetApiExtensions
    {
        public static NuGetSpec ParseSpec(this INuGetApi api, byte[] content)
        {
            api.AssertNotNull(nameof(api));
            content.AssertNotNull(nameof(content));

            using (var stream = new MemoryStream(content))
            {
                return api.ParseSpec(stream);
            }
        }
    }
}
