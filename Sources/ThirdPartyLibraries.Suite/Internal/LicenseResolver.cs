using System;
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
        }

        public IUnityContainer Container { get; }

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

            // check by host
            var host = new Uri(url).Host.ToLowerInvariant();
            if (Container.IsRegistered<ILicenseSourceByUrl>(host))
            {
                return await Container.Resolve<ILicenseSourceByUrl>(host).DownloadByUrlAsync(url, token);
            }

            if (Container.IsRegistered<ILicenseCodeSource>(host))
            {
                code = await Container.Resolve<ILicenseCodeSource>(host).ResolveLicenseCodeAsync(url, token);
            }

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

        public async Task<LicenseInfo> DownloadByCodeAsync(string code, CancellationToken token)
        {
            code.AssertNotNull(nameof(code));

            // check static
            var license = await Container.Resolve<IStaticLicenseSource>().DownloadLicenseByCodeAsync(code, token);
            if (license == null)
            {
                // check by code
                var name = code.ToUpperInvariant();
                var source = Container.IsRegistered<IFullLicenseSource>(name)
                    ? Container.Resolve<IFullLicenseSource>(name)
                    : Container.Resolve<IFullLicenseSource>();

                license = await source.DownloadLicenseByCodeAsync(code, token);
            }

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
    }
}
