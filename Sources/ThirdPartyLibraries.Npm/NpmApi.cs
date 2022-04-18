using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
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
            using (var response = await client.GetAsync(url, token).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                await response.AssertStatusCodeOk().ConfigureAwait(false);

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
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
            using (var stream = await client.GetStreamAsync(packageUrl).ConfigureAwait(false))
            {
                var content = await stream.ToArrayAsync(token).ConfigureAwait(false);
                return new NpmPackageFile(fileName, content);
            }
        }

        public byte[] ExtractPackageJson(byte[] packageContent)
        {
            packageContent.AssertNotNull(nameof(packageContent));

            var result = ExtractPackageFile(packageContent, PackageJsonParser.FileName);
            if (result == null)
            {
                throw new InvalidOperationException(PackageJsonParser.FileName + " not found in the package.");
            }

            return result;
        }

        public byte[] LoadFileContent(byte[] packageContent, string fileName)
        {
            packageContent.AssertNotNull(nameof(packageContent));
            fileName.AssertNotNull(nameof(fileName));

            return ExtractPackageFile(packageContent, fileName);
        }

        public string ResolveNpmRoot()
        {
            var info = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                ErrorDialog = false,
                Environment =
                {
                    { "npm_config_loglevel", "silent" }
                }
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                info.FileName = "cmd";
                info.Arguments = "/c \"npm root -g\"";
                info.LoadUserProfile = true;
            }
            else
            {
                info.FileName = "npm";
                info.Arguments = "root -g";
            }

            Process process;
            try
            {
                process = Process.Start(info);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Fail to execute command [npm root -g]: {0}".FormatWith(ex.Message), ex);
            }

            string result;
            using (process)
            {
                process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
                result = process.StandardOutput.ReadToEnd().Trim('\r', '\n');

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("The command [npm root -g] exited with code {0}.".FormatWith(process.ExitCode));
                }
            }

            ////if (!Directory.Exists(result))
            ////{
            ////    throw new DirectoryNotFoundException(string.Format("Npm root directory {0} not found.", result));
            ////}

            return result;
        }

        private static byte[] ExtractPackageFile(byte[] packageContent, string fileName)
        {
            using (var zip = new TarGZip(packageContent))
            {
                if (!zip.SeekToEntry(fileName))
                {
                    return null;
                }

                return zip.GetCurrentEntryContent();
            }
        }
    }
}
