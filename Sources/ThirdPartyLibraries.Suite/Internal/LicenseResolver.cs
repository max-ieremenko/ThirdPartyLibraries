using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Generic;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal sealed class LicenseResolver : ILicenseResolver
    {
        public LicenseResolver(IServiceProvider serviceProvider)
        {
            serviceProvider.AssertNotNull(nameof(serviceProvider));

            ServiceProvider = serviceProvider;
            Cache = serviceProvider.GetRequiredService<ILicenseCache>();
        }

        public IServiceProvider ServiceProvider { get; }

        public ILicenseCache Cache { get; }

        public async Task<LicenseInfo> ResolveByUrlAsync(string url, CancellationToken token)
        {
            url.AssertNotNull(nameof(url));

            // check static
            var code = await ServiceProvider.GetRequiredService<IStaticLicenseSource>().ResolveLicenseCodeAsync(url, token).ConfigureAwait(false);
            if (!code.IsNullOrEmpty())
            {
                return new LicenseInfo
                {
                    Code = code,
                    CodeHRef = url
                };
            }

            if (Cache.TryGetByUrl(url, out var result))
            {
                return result;
            }

            result = await DownloadByUrlAsync(url, token).ConfigureAwait(false);

            // https://github.community/t5/GitHub-API-Development-and/API-rate-limit-is-60-for-authenticated-request/m-p/43733#M3883
            Cache.AddByUrl(url, result);

            return result;
        }

        public async Task<LicenseInfo> DownloadByCodeAsync(string code, CancellationToken token)
        {
            code.AssertNotNull(nameof(code));

            // check static
            var license = await ServiceProvider.GetRequiredService<IStaticLicenseSource>().DownloadLicenseByCodeAsync(code, token).ConfigureAwait(false);
            if (license != null)
            {
                return Convert(license);
            }

            if (Cache.TryGetByCode(code, out var result))
            {
                return result;
            }

            result = await DownloadLicenseByCodeAsync(code, token).ConfigureAwait(false);

            Cache.AddByCode(code, result);

            return result;
        }

        private static LicenseInfo Convert(GenericLicense license)
        {
            if (license == null)
            {
                return null;
            }

            return new LicenseInfo
            {
                Code = license.Code,
                FullName = license.FullName,
                FileHRef = license.FileHRef,
                FileName = license.FileName,
                FileContent = license.FileContent
            };
        }

        private async Task<LicenseInfo> DownloadLicenseByCodeAsync(string code, CancellationToken token)
        {
            var license = await ServiceProvider
                .GetRequiredService<IStaticLicenseSource>()
                .DownloadLicenseByCodeAsync(code, token)
                .ConfigureAwait(false);

            if (license != null)
            {
                return Convert(license);
            }

            var source = ServiceProvider.GetKeyedService<IFullLicenseSource>(code) ?? ServiceProvider.GetRequiredService<IFullLicenseSource>();
            license = await source.DownloadLicenseByCodeAsync(code, token).ConfigureAwait(false);

            return Convert(license);
        }

        private async Task<LicenseInfo> DownloadByUrlAsync(string url, CancellationToken token)
        {
            // check by host
            var host = new Uri(url).Host.ToLowerInvariant();
            
            var licenseSourceByUrl = ServiceProvider.GetKeyedService<ILicenseSourceByUrl>(host);
            if (licenseSourceByUrl != null)
            {
                return await licenseSourceByUrl.DownloadByUrlAsync(url, token).ConfigureAwait(false);
            }

            var licenseCodeSource = ServiceProvider.GetKeyedService<ILicenseCodeSource>(host);
            if (licenseCodeSource == null)
            {
                return null;
            }

            var code = await licenseCodeSource.ResolveLicenseCodeAsync(url, token).ConfigureAwait(false);

            if (code.IsNullOrEmpty())
            {
                return null;
            }

            return new LicenseInfo
            {
                Code = code,
                CodeHRef = url
            };
        }
    }
}
