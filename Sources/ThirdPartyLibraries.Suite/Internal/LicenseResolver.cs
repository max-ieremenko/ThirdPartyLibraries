﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Generic;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal
{
    internal sealed class LicenseResolver : ILicenseResolver
    {
        public LicenseResolver(IUnityContainer container)
        {
            container.AssertNotNull(nameof(container));

            Container = container;
            Cache = container.Resolve<ILicenseCache>();
        }

        public IUnityContainer Container { get; }

        public ILicenseCache Cache { get; }

        public async Task<LicenseInfo> ResolveByUrlAsync(string url, CancellationToken token)
        {
            url.AssertNotNull(nameof(url));

            // check static
            var code = await Container.Resolve<IStaticLicenseSource>().ResolveLicenseCodeAsync(url, token);
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

            result = await DownloadByUrlAsync(url, token);

            // https://github.community/t5/GitHub-API-Development-and/API-rate-limit-is-60-for-authenticated-request/m-p/43733#M3883
            Cache.AddByUrl(url, result);

            return result;
        }

        public async Task<LicenseInfo> DownloadByCodeAsync(string code, CancellationToken token)
        {
            code.AssertNotNull(nameof(code));

            // check static
            var license = await Container.Resolve<IStaticLicenseSource>().DownloadLicenseByCodeAsync(code, token);
            if (license != null)
            {
                return Convert(license);
            }

            if (Cache.TryGetByCode(code, out var result))
            {
                return result;
            }

            result = await DownloadLicenseByCodeAsync(code, token);

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
            // check by code
            var name = code.ToUpperInvariant();
            var source = Container.IsRegistered<IFullLicenseSource>(name)
                ? Container.Resolve<IFullLicenseSource>(name)
                : Container.Resolve<IFullLicenseSource>();

            var license = await source.DownloadLicenseByCodeAsync(code, token);
            return Convert(license);
        }

        private async Task<LicenseInfo> DownloadByUrlAsync(string url, CancellationToken token)
        {
            // check by host
            var host = new Uri(url).Host.ToLowerInvariant();
            if (Container.IsRegistered<ILicenseSourceByUrl>(host))
            {
                return await Container.Resolve<ILicenseSourceByUrl>(host).DownloadByUrlAsync(url, token);
            }

            if (!Container.IsRegistered<ILicenseCodeSource>(host))
            {
                return null;
            }

            var code = await Container.Resolve<ILicenseCodeSource>(host).ResolveLicenseCodeAsync(url, token);

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
