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

namespace ThirdPartyLibraries.Suite.Commands
{
    public sealed class GenerateCommand : ICommand
    {
        internal const string OutputFileName = "ThirdPartyNotices.txt";

        public IList<string> AppNames { get; } = new List<string>();
        
        public string To { get; set; }

        public async ValueTask<bool> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            var repository = serviceProvider.GetRequiredService<IPackageRepository>();
            var state = new GenerateCommandState(repository, To, serviceProvider.GetRequiredService<ILogger>());
            var packages = await LoadAllPackagesNoticesAsync(repository, token).ConfigureAwait(false);

            var rootContext = new ThirdPartyNoticesContext();

            foreach (var package in packages)
            {
                var license = await state.GetLicensesAsync(package.LicenseCode, token).ConfigureAwait(false);

                var packageContext = new ThirdPartyNoticesPackageContext
                {
                    Name = package.Name,
                    License = license,
                    Copyright = package.Copyright,
                    HRef = package.HRef,
                    Author = package.Author,
                    ThirdPartyNotices = package.ThirdPartyNotices
                };

                rootContext.Packages.Add(packageContext);
                license.Packages.Add(packageContext);
            }

            rootContext.Licenses.AddRange(state.Licenses.OrderBy(i => i.FullName));

            var template = await repository.Storage.GetOrCreateThirdPartyNoticesTemplateAsync(token).ConfigureAwait(false);
            var fileName = Path.Combine(To, OutputFileName);
            using (var file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                DotLiquidTemplate.RenderTo(file, template, rootContext);
            }

            return true;
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
    }
}
