using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet
{
    internal sealed class NuGetApi : INuGetApi
    {
        public const string Host = "https://" + KnownHosts.NuGetApi;

        public NuGetApi(Func<HttpClient> httpClientFactory)
        {
            httpClientFactory.AssertNotNull(nameof(httpClientFactory));

            HttpClientFactory = httpClientFactory;
        }

        public Func<HttpClient> HttpClientFactory { get; }

        public async Task<byte[]> ExtractSpecAsync(NuGetPackageId package, byte[] packageContent, CancellationToken token)
        {
            packageContent.AssertNotNull(nameof(packageContent));

            var fileName = "{0}.nuspec".FormatWith(package.Name);
            var result = await LoadFileContentAsync(packageContent, fileName, token);

            if (result == null)
            {
                throw new InvalidOperationException(fileName + " not found in the package.");
            }

            return result;
        }

        public NuGetSpec ParseSpec(Stream content)
        {
            content.AssertNotNull(nameof(content));

            return NuGetSpecParser.FromStream(content);
        }

        // https://docs.microsoft.com/en-us/nuget/nuget-org/licenses.nuget.org
        public async Task<string> ResolveLicenseCodeAsync(string licenseUrl, CancellationToken token)
        {
            licenseUrl.AssertNotNull(nameof(licenseUrl));

            var code = ExtractLicenseCode(licenseUrl);
            if (string.IsNullOrEmpty(code))
            {
                return null;
            }

            using (var client = HttpClientFactory())
            using (var response = await client.GetAsync(licenseUrl, token))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                await response.AssertStatusCodeOk();
            }

            return code;
        }

        public async Task<byte[]> DownloadPackageAsync(NuGetPackageId package, bool allowToUseLocalCache, CancellationToken token)
        {
            if (allowToUseLocalCache)
            {
                var path = GetLocalCachePath(package);
                if (path != null)
                {
                    return LoadPackageFromLocalCache(path, package.Name, package.Version);
                }
            }

            var url = GetPackageUri(package);

            using (var client = HttpClientFactory())
            using (var response = await client.GetAsync(url, token))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                await response.AssertStatusCodeOk();

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    return await stream.ToArrayAsync(token);
                }
            }
        }

        public async Task<byte[]> LoadFileContentAsync(byte[] packageContent, string fileName, CancellationToken token)
        {
            packageContent.AssertNotNull(nameof(packageContent));

            using (var zip = new ZipArchive(new MemoryStream(packageContent), ZipArchiveMode.Read, false))
            {
                var entryName = fileName.Replace("\\", "/");
                var entry = zip.Entries.FirstOrDefault(i => i.FullName.EqualsIgnoreCase(entryName));
                if (entry == null)
                {
                    return null;
                }

                using (var content = entry.Open())
                {
                    return await content.ToArrayAsync(token);
                }
            }
        }

        internal static string ExtractLicenseCode(string licenseUrl)
        {
            var expression = new UriBuilder(licenseUrl).Path.Trim();
            if (expression.Length == 1)
            {
                return null;
            }

            expression = HttpUtility.UrlDecode(expression.Substring(1).Trim());
            return expression.Trim();
        }

        private static string GetLocalCachePath(NuGetPackageId package)
        {
            var path = Path.Combine(
                Environment.GetEnvironmentVariable("USERPROFILE"),
                @".nuget\packages",
                package.Name,
                package.Version);

            return Directory.Exists(path) ? path : null;
        }

        private static byte[] LoadPackageFromLocalCache(string path, string packageName, string version)
        {
            var fileName = Path.Combine(path, "{0}.{1}.nupkg".FormatWith(packageName, version));

            if (!File.Exists(fileName))
            {
                return null;
            }

            return File.ReadAllBytes(fileName);
        }
        
        private static Uri GetPackageUri(NuGetPackageId package) => new Uri(
            new Uri(Host), 
            "v3-flatcontainer/{0}/{1}/{0}.{1}.nupkg".FormatWith(package.Name.ToLowerInvariant(), package.Version.ToLowerInvariant()));
    }
}
