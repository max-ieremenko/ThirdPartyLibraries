using System.IO;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm
{
    public static class NpmApiExtensions
    {
        public static PackageJson ParsePackageJson(this INpmApi api, byte[] content)
        {
            api.AssertNotNull(nameof(api));
            content.AssertNotNull(nameof(content));

            using (var stream = new MemoryStream(content))
            {
                return api.ParsePackageJson(stream);
            }
        }
    }
}
