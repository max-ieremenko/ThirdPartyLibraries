using System;
using System.IO;
using System.IO.Compression;
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

        public async Task<byte[]> LoadSpecAsync(NuGetPackageId package, bool allowToUseLocalCache, CancellationToken token)
        {
            if (allowToUseLocalCache)
            {
                var path = GetLocalCachePath(package);
                if (path != null)
                {
                    return LoadSpecFromLocalCache(path, package.Name);
                }
            }

            var url = new Uri(new Uri(Host), "v3-flatcontainer/{0}/{1}/{0}.nuspec".FormatWith(package.Name, package.Version));

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

        public async Task<byte[]> LoadFileContentAsync(NuGetPackageId package, string fileName, bool allowToUseLocalCache, CancellationToken token)
        {
            if (allowToUseLocalCache)
            {
                var path = GetLocalCachePath(package);
                if (path != null)
                {
                    return LoadFileContentFromLocalCache(path, fileName);
                }
            }

            Func<ZipArchive, Task<byte[]>> callback = async zip =>
            {
                var entryName = fileName.Replace("\\", "/");
                var entry = zip.GetEntry(entryName);
                if (entry == null)
                {
                    return null;
                }

                using (var content = entry.Open())
                {
                    return await content.ToArrayAsync(token);
                }
            };

            return await AnalyzePackageContentAsync(package, callback, token);
        }

        public async Task<NuGetPackageLicenseFile?> TryFindLicenseFileAsync(NuGetPackageId package, bool allowToUseLocalCache, CancellationToken token)
        {
            if (allowToUseLocalCache)
            {
                var path = GetLocalCachePath(package);
                if (path != null)
                {
                    return FindLicenseFileInLocalCache(path);
                }
            }

            Func<ZipArchive, Task<NuGetPackageLicenseFile?>> callback = async zip =>
            {
                foreach (var name in GetLicenseFileNames())
                {
                    var entry = zip.GetEntry(name);
                    if (entry != null)
                    {
                        using (var content = entry.Open())
                        {
                            return new NuGetPackageLicenseFile
                            {
                                Name = name,
                                Content = await content.ToArrayAsync(token)
                            };
                        }
                    }
                }

                return null;
            };

            return await AnalyzePackageContentAsync(package, callback, token);
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

        private static byte[] LoadSpecFromLocalCache(string path, string packageName)
        {
            var fileName = Path.Combine(path, "{0}.nuspec".FormatWith(packageName));

            if (!File.Exists(fileName))
            {
                return null;
            }

            return File.ReadAllBytes(fileName);
        }

        private static byte[] LoadFileContentFromLocalCache(string path, string fileName)
        {
            fileName = Path.Combine(path, fileName);

            if (!File.Exists(fileName))
            {
                return null;
            }

            return File.ReadAllBytes(fileName);
        }

        private static NuGetPackageLicenseFile? FindLicenseFileInLocalCache(string path)
        {
            foreach (var name in GetLicenseFileNames())
            {
                var content = LoadFileContentFromLocalCache(path, name);
                if (content != null)
                {
                    return new NuGetPackageLicenseFile
                    {
                        Name = name,
                        Content = content
                    };
                }
            }

            return null;
        }

        private static string[] GetLicenseFileNames() => new[] { "LICENSE.md", "LICENSE.txt", "LICENSE", "LICENSE.rtf" };

        private async Task<TResult> AnalyzePackageContentAsync<TResult>(NuGetPackageId package, Func<ZipArchive, Task<TResult>> callback, CancellationToken token)
        {
            var url = new Uri(new Uri(Host), "v3-flatcontainer/{0}/{1}/{0}.nupkg".FormatWith(package.Name, package.Version));

            using (var client = HttpClientFactory())
            using (var response = await client.GetAsync(url, token))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return default;
                }

                await response.AssertStatusCodeOk();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var zip = new ZipArchive(stream))
                {
                    return await callback(zip);
                }
            }
        }
    }
}
