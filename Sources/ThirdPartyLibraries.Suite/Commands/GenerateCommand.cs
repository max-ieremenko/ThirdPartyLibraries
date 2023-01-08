using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands;

public sealed class GenerateCommand : ICommand
{
    internal const string OutputFileName = "ThirdPartyNotices.txt";

    public IList<string> AppNames { get; } = new List<string>();

    public string Title { get; set; }

    public string To { get; set; }

    public string ToFileName { get; set; }

    public string Template { get; set; }

    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        var repository = serviceProvider.GetRequiredService<IPackageRepository>();
        var logger = serviceProvider.GetRequiredService<ILogger>();

        Hello(logger, repository);

        var state = new GenerateCommandState(repository, serviceProvider.GetKeyedService<ILicenseSourceByUrl>, To, logger);
        var packages = await LoadAllPackagesNoticesAsync(repository, token).ConfigureAwait(false);

        var rootContext = new ThirdPartyNoticesContext
        {
            Title = string.IsNullOrWhiteSpace(Title) ? AppNames[0] : Title
        };

        foreach (var package in packages)
        {
            var license = await state.GetLicensesAsync(package.LicenseCode, token).ConfigureAwait(false);
            var packageLicense = await state.GetPackageLicenseAsync(package, token).ConfigureAwait(false);

            var packageContext = new ThirdPartyNoticesPackageContext
            {
                Name = package.Name,
                License = license,
                PackageLicense = packageLicense,
                Copyright = package.Copyright,
                HRef = package.HRef,
                Author = package.Author,
                ThirdPartyNotices = package.ThirdPartyNotices
            };

            rootContext.Packages.Add(packageContext);
            license.Packages.Add(packageContext);
        }

        rootContext.Licenses.AddRange(state.Licenses.OrderBy(i => i.FullName));

        await state.AlignFileNamesAsync(token).ConfigureAwait(false);

        string template;
        if (string.IsNullOrEmpty(Template))
        {
            template = await repository.Storage.GetOrCreateThirdPartyNoticesTemplateAsync(token).ConfigureAwait(false);
        }
        else
        {
            template = await File.ReadAllTextAsync(Template, token).ConfigureAwait(false);
        }

        Directory.CreateDirectory(To);
        var fileName = GetOutputFileName();
        using (var file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
        {
            DotLiquidTemplate.RenderTo(file, template, rootContext);
        }

        state.CleanUpLicensesDirectory();
        await CopyLicenseFilesAsync(fileName, state, token).ConfigureAwait(false);
    }

    private void Hello(ILogger logger, IPackageRepository repository)
    {
        logger.Info("generate third party notices for " + string.Join(", ", AppNames));
        using (logger.Indent())
        {
            logger.Info("repository {0}".FormatWith(repository.Storage.ConnectionString));
            logger.Info("to {0}".FormatWith(GetOutputFileName()));
            if (!string.IsNullOrEmpty(Template))
            {
                logger.Info("template {0}".FormatWith(Template));
            }
        }
    }

    private async Task<IList<Package>> LoadAllPackagesNoticesAsync(IPackageRepository repository, CancellationToken token)
    {
        var libraries = await repository.Storage.GetAllLibrariesAsync(token).ConfigureAwait(false);
        var result = new List<Package>(libraries.Count);

        var sorted = libraries
            .OrderBy(i => i.Name)
            .ThenBy(i => i.Version)
            .ThenBy(i => i.SourceCode);

        foreach (var id in sorted)
        {
            var package = await repository.LoadPackageAsync(id, token).ConfigureAwait(false);
            if (UsePackage(package))
            {
                result.Add(package);
            }
        }

        return result;
    }

    private bool UsePackage(Package package)
    {
        if (package.LicenseCode.IsNullOrEmpty())
        {
            return false;
        }

        foreach (var appName in AppNames)
        {
            var appIndex = package.UsedBy.IndexOf(i => appName.EqualsIgnoreCase(i.Name) && !i.InternalOnly);
            if (appIndex >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private async Task CopyLicenseFilesAsync(string reportFileName, GenerateCommandState state, CancellationToken token)
    {
        var reportContent = File.ReadAllText(reportFileName);

        foreach (var fileName in state.GetAllLicenseFiles())
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            if (reportContent.Contains(fileName, StringComparison.OrdinalIgnoreCase))
            {
                await state.CopyToLicensesDirectory(fileName, token).ConfigureAwait(false);
            }
        }
    }

    private string GetOutputFileName() => Path.Combine(To, string.IsNullOrEmpty(ToFileName) ? OutputFileName : ToFileName);
}