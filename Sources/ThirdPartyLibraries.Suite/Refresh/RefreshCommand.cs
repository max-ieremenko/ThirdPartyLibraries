using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Refresh.Internal;

namespace ThirdPartyLibraries.Suite.Refresh;

public sealed class RefreshCommand : ICommand
{
    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();

        Hello(serviceProvider.GetRequiredService<ILogger>(), storage.ConnectionString);

        var context = new RootReadMeContext();
        
        await AddPackagesAsync(storage, serviceProvider.GetRequiredService<IPackageReadMeUpdater>(), context, token).ConfigureAwait(false);
        await AddLicensesAsync(storage, context, token).ConfigureAwait(false);

        await storage.WriteRootReadMeAsync(context, token).ConfigureAwait(false);
    }

    private static void Hello(ILogger logger, string storageConnectionString)
    {
        logger.Info("update .md files");
        using (logger.Indent())
        {
            logger.Info($"repository {storageConnectionString}");
        }
    }

    private static async Task AddPackagesAsync(
        IStorage storage,
        IPackageReadMeUpdater readMeUpdater,
        RootReadMeContext rootContext,
        CancellationToken token)
    {
        var libraries = await storage.GetAllLibrariesAsync(token).ConfigureAwait(false);

        for (var i = 0; i < libraries.Count; i++)
        {
            var context = await readMeUpdater.UpdateAsync(libraries[i], token).ConfigureAwait(false);
            if (context != null)
            {
                rootContext.Packages.Add(context);
                if (!context.IsApproved)
                {
                    rootContext.TodoPackages.Add(context);
                }
            }
        }
    }

    private static async Task AddLicensesAsync(IStorage storage, RootReadMeContext rootContext, CancellationToken token)
    {
        var allCodes = await storage.GetAllLicenseCodesAsync(token).ConfigureAwait(false);
        var licenseByCode = new Dictionary<string, RootReadMeLicenseContext>(allCodes.Count, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < allCodes.Count; i++)
        {
            var code = allCodes[i];

            var index = await storage.ReadLicenseIndexJsonAsync(code, token).ConfigureAwait(false);
            if (index != null)
            {
                var context = new RootReadMeLicenseContext
                {
                    Code = index.Code,
                    RequiresApproval = index.RequiresApproval,
                    RequiresThirdPartyNotices = index.RequiresThirdPartyNotices,
                    LocalHRef = storage.GetLicenseLocalHRef(index.Code)
                };
                licenseByCode.Add(code, context);
            }
        }

        for (var i = 0; i < rootContext.Packages.Count; i++)
        {
            var codes = LicenseCode.FromText(rootContext.Packages[i].License);
            for (var j = 0; j < codes.Codes.Length; j++)
            {
                var code = codes.Codes[j];

                if (licenseByCode.TryGetValue(code, out var context))
                {
                    context.PackagesCount++;
                }
            }
        }

        var licenses = licenseByCode
            .Values
            .OrderBy(i => i!.Code);

        foreach (var license in licenses)
        {
            rootContext.Licenses.Add(license!);
        }
    }
}