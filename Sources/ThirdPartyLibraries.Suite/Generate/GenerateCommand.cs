using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Generate.Internal;

namespace ThirdPartyLibraries.Suite.Generate;

public sealed class GenerateCommand : ICommand
{
    internal const string OutputFileName = "ThirdPartyNotices.txt";

    public List<string> AppNames { get; } = new();

    public string? Title { get; set; }

    public string To { get; set; } = null!;

    public string? ToFileName { get; set; }

    public string? Template { get; set; }

    public async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();

        Hello(serviceProvider.GetRequiredService<ILogger>(), storage.ConnectionString);

        var fileNameResolver = serviceProvider.GetRequiredService<ILicenseFileNameResolver>();
        var state = await LoadStateAsync(
                storage,
                serviceProvider.GetRequiredService<ILicenseNoticesLoader>(),
                serviceProvider.GetRequiredService<IPackageNoticesLoader>(),
                fileNameResolver,
                token)
            .ConfigureAwait(false);

        fileNameResolver.Seal();

        var context = CreateContext(state, fileNameResolver);

        var reportFileName = await RenderAsync(storage, context, token).ConfigureAwait(false);

        await CopyLicenseFilesAsync(storage, reportFileName, context, state, token).ConfigureAwait(false);
    }

    private void Hello(ILogger logger, string storageConnectionString)
    {
        logger.Info("generate third party notices for " + string.Join(", ", AppNames));
        using (logger.Indent())
        {
            logger.Info($"repository {storageConnectionString}");
            logger.Info($"to {GetOutputFileName()}");
            if (!string.IsNullOrEmpty(Template))
            {
                logger.Info($"template {Template}");
            }
        }
    }

    private string GetOutputFileName() => Path.Combine(To, string.IsNullOrEmpty(ToFileName) ? OutputFileName : ToFileName);

    private ThirdPartyNoticesContext CreateContext(GenerateCommandState state, ILicenseFileNameResolver fileNameResolver)
    {
        var rootContext = new ThirdPartyNoticesContext
        {
            Title = string.IsNullOrWhiteSpace(Title) ? AppNames[0] : Title
        };

        var licenseByCode = new Dictionary<LicenseCode, ThirdPartyNoticesLicenseContext>(state.LicenseByCode.Count);

        for (var i = 0; i < state.Packages.Count; i++)
        {
            var package = state.Packages[i];

            var licenseCode = LicenseCode.FromText(package.LicenseCode);
            if (!licenseByCode.TryGetValue(licenseCode, out var license))
            {
                license = state.CreateLicenseContext(licenseCode, fileNameResolver);
                licenseByCode.Add(licenseCode, license);
            }

            var packageContext = state.CreatePackageContext(package, license, fileNameResolver);

            license.Packages.Add(packageContext);
            rootContext.Packages.Add(packageContext);
        }

        foreach (var license in licenseByCode.Values.OrderBy(i => i.FullName))
        {
            rootContext.Licenses.Add(license);
        }

        return rootContext;
    }

    private async Task<GenerateCommandState> LoadStateAsync(
        IStorage storage,
        ILicenseNoticesLoader licenseLoader,
        IPackageNoticesLoader packageLoader,
        ILicenseFileNameResolver fileNameResolver,
        CancellationToken token)
    {
        var result = new GenerateCommandState();

        var libraries = await storage.GetAllLibrariesAsync(token).ConfigureAwait(false);
        for (var i = 0; i < libraries.Count; i++)
        {
            var package = await packageLoader.LoadAsync(libraries[i], AppNames, token).ConfigureAwait(false);
            if (package == null)
            {
                continue;
            }

            var licenseCode = LicenseCode.FromText(package.LicenseCode);
            if (licenseCode.IsEmpty)
            {
                continue;
            }

            result.Packages.Add(package);
            if (package.LicenseFile != null)
            {
                fileNameResolver.AddFile(package.LicenseFile);
            }

            if (!result.LicenseByCode.ContainsKey(licenseCode))
            {
                var license = await licenseLoader.LoadAsync(licenseCode, token).ConfigureAwait(false);
                result.LicenseByCode.Add(licenseCode, license);

                foreach (var file in license.Files)
                {
                    fileNameResolver.AddFile(file);
                }
            }
        }

        return result;
    }

    private async Task<string> RenderAsync(IStorage storage, ThirdPartyNoticesContext context, CancellationToken token)
    {
        string template;
        if (string.IsNullOrEmpty(Template))
        {
            template = await storage.GetOrCreateThirdPartyNoticesTemplateAsync(token).ConfigureAwait(false);
        }
        else
        {
            template = await File.ReadAllTextAsync(Template, token).ConfigureAwait(false);
        }

        Directory.CreateDirectory(To);
        var fileName = GetOutputFileName();
        using (var file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
        {
            DotLiquidTemplate.RenderTo(file, template, context);
        }

        return fileName;
    }

    private async Task CopyLicenseFilesAsync(
        IStorage storage,
        string reportFileName,
        ThirdPartyNoticesContext context,
        GenerateCommandState state,
        CancellationToken token)
    {
        var path = Path.Combine(To, GenerateCommandState.LicensesDirectory);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        var distinct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directoryExists = false;

        var reportContent = await File.ReadAllTextAsync(reportFileName, token).ConfigureAwait(false);
        for (var i = 0; i < context.Licenses.Count; i++)
        {
            var fileNames = context.Licenses[i].FileNames;
            for (var j = 0; j < fileNames.Count; j++)
            {
                var fileName = fileNames[j];
                if (reportContent.Contains(fileName, StringComparison.OrdinalIgnoreCase) && distinct.Add(fileName))
                {
                    if (!directoryExists)
                    {
                        Directory.CreateDirectory(path);
                        directoryExists = true;
                    }

                    await state.CopyFileAsync(storage, To, fileName, token).ConfigureAwait(false);
                }
            }
        }

        for (var i = 0; i < context.Packages.Count; i++)
        {
            var fileNames = context.Packages[i].PackageLicense.FileNames;
            for (var j = 0; j < fileNames.Count; j++)
            {
                var fileName = fileNames[j];
                if (reportContent.Contains(fileName, StringComparison.OrdinalIgnoreCase) && distinct.Add(fileName))
                {
                    if (!directoryExists)
                    {
                        Directory.CreateDirectory(path);
                        directoryExists = true;
                    }

                    await state.CopyFileAsync(storage, To, fileName, token).ConfigureAwait(false);
                }
            }
        }
    }
}