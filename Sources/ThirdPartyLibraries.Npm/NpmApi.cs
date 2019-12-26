using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm
{
    internal sealed class NpmApi : INpmApi
    {
        public const string Host = "https://" + KnownHosts.NpmRegistry;

        public NpmApi(Func<HttpClient> httpClientFactory)
        {
            httpClientFactory.AssertNotNull(nameof(httpClientFactory));

            HttpClientFactory = httpClientFactory;
        }

        public Func<HttpClient> HttpClientFactory { get; }

        public PackageJson ParsePackageJson(Stream content)
        {
            content.AssertNotNull(nameof(content));

            var parser = new PackageJsonParser(content.JsonDeserialize<JObject>());
            return new PackageJson
            {
                Name = parser.GetName(),
                Version = parser.GetVersion(),
                HomePage = parser.GetHomePage(),
                Authors = parser.GetAuthors(),
                Description = parser.GetDescription(),
                License = parser.GetLicense(),
                Repository = parser.GetRepository(),
                PackageHRef = new Uri(new Uri("https://" + KnownHosts.Npm), "package/{0}/v/{1}".FormatWith(parser.GetName(), parser.GetVersion())).ToString()
            };
        }

        public async Task<NpmPackageFile?> DownloadPackageAsync(NpmPackageId id, CancellationToken token)
        {
            // does not work: https://registry.npmjs.org/@types%2Fangular/1.6.55
            var url = new Uri(new Uri(Host), UrlEncoder.Default.Encode(id.Name));

            JObject index;
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
                    index = stream.JsonDeserialize<JObject>();
                }
            }

            var version = index.Value<JObject>("versions").Value<JObject>(id.Version);
            if (version == null)
            {
                return null;
            }

            // https://registry.npmjs.org/@types/angular/-/angular-1.6.55.tgz
            var packageUrl = new Uri(version.Value<JObject>("dist").Value<string>("tarball"));

            var fileName = packageUrl.LocalPath.Substring(packageUrl.LocalPath.LastIndexOf('/') + 1);

            using (var client = HttpClientFactory())
            using (var stream = await client.GetStreamAsync(packageUrl))
            {
                var content = await stream.ToArrayAsync(token);
                return new NpmPackageFile(fileName, content);
            }
        }

        public async Task<byte[]> ExtractPackageJsonAsync(byte[] packageContent, CancellationToken token)
        {
            packageContent.AssertNotNull(nameof(packageContent));

            var result = await ExtractPackageFileAsync(packageContent, PackageJsonParser.FileName);
            if (result == null)
            {
                throw new InvalidOperationException(PackageJsonParser.FileName + " not found in the package.");
            }

            return result;
        }

        public Task<byte[]> LoadFileContentAsync(byte[] packageContent, string fileName, CancellationToken token)
        {
            packageContent.AssertNotNull(nameof(packageContent));
            fileName.AssertNotNull(nameof(fileName));

            return ExtractPackageFileAsync(packageContent, fileName);
        }

        public async Task<NpmPackageFile?> TryFindLicenseFileAsync(byte[] packageContent, CancellationToken token)
        {
            packageContent.AssertNotNull(nameof(packageContent));

            foreach (var fileName in GetLicenseFileNames())
            {
                var content = await LoadFileContentAsync(packageContent, fileName, token);
                if (content != null)
                {
                    return new NpmPackageFile(fileName, content);
                }
            }

            return null;
        }

        private static async Task<byte[]> ExtractPackageFileAsync(byte[] packageContent, string fileName)
        {
            await using (var zip = new TarGZip(packageContent))
            {
                if (!zip.SeekToEntry(fileName))
                {
                    return null;
                }

                return zip.GetCurrentEntryContent();
            }
        }

        private static string[] GetLicenseFileNames() => new[] { "LICENSE.md", "LICENSE.txt", "LICENSE", "LICENSE.rtf" };
    }
}
