using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Generic
{
    internal sealed class CodeProjectApi : ILicenseCodeSource, IFullLicenseSource
    {
        public const string LicenseCode = "CPOL";

        public CodeProjectApi(Func<HttpClient> httpClientFactory)
        {
            httpClientFactory.AssertNotNull(nameof(httpClientFactory));

            HttpClientFactory = httpClientFactory;
        }

        public Func<HttpClient> HttpClientFactory { get; }

        public Task<string> ResolveLicenseCodeAsync(string licenseUrl, CancellationToken token)
        {
            string result = null;

            var url = new Uri(licenseUrl);
            if (KnownHosts.CodeProject.EqualsIgnoreCase(url.Host) && "/info/cpol10.aspx".EqualsIgnoreCase(url.AbsolutePath))
            {
                result = LicenseCode;
            }

            return Task.FromResult(result);
        }

        public async Task<GenericLicense> DownloadLicenseByCodeAsync(string licenseCode, CancellationToken token)
        {
            var result = new GenericLicense
            {
                Code = LicenseCode,
                FullName = "Code Project Open License (CPOL) 1.02",
                FileHRef = "https://www.codeproject.com/info/cpol10.aspx"
            };

            using (var client = HttpClientFactory())
            using (var response = await client.GetAsync("https://www.codeproject.com/info/CPOL.zip", token).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                await response.AssertStatusCodeOk().ConfigureAwait(false);

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var zip = new ZipArchive(stream))
                {
                    var entry = zip.Entries.Count == 1 ? zip.Entries[0] : zip.Entries.First(i => ".htm".EqualsIgnoreCase(Path.GetExtension(i.Name)));
                    using (var entryStream = entry.Open())
                    {
                        result.FileContent = await entryStream.ToArrayAsync(token).ConfigureAwait(false);
                        result.FileName = entry.Name;
                    }
                }
            }

            return result;
        }
    }
}
