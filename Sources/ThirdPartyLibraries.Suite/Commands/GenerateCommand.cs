﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Repository.Template;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;
using Unity;

namespace ThirdPartyLibraries.Suite.Commands
{
    public sealed class GenerateCommand : ICommand
    {
        internal const string OutputFileName = "ThirdPartyNotices.txt";

        public GenerateCommand(IUnityContainer container, ILogger logger)
        {
            container.AssertNotNull(nameof(container));
            logger.AssertNotNull(nameof(logger));

            Container = container;
            Logger = logger;
        }

        public IUnityContainer Container { get; }

        public ILogger Logger { get; }

        public IList<string> AppNames { get; } = new List<string>();
        
        public string To { get; set; }

        public async ValueTask<bool> ExecuteAsync(CancellationToken token)
        {
            var repository = Container.Resolve<IPackageRepository>();
            var state = new GenerateCommandState(repository, To, Logger);
            var packages = await LoadAllPackagesNoticesAsync(repository, token);

            var rootContext = new ThirdPartyNoticesContext();

            foreach (var package in packages)
            {
                var license = await state.GetLicensesAsync(package.LicenseCode, token);

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

            var template = await repository.Storage.GetOrCreateThirdPartyNoticesTemplateAsync(token);
            var fileName = Path.Combine(To, OutputFileName);
            using (var file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                DotLiquidTemplate.RenderTo(file, template, rootContext);
            }

            return true;
        }

        private async Task<IList<Package>> LoadAllPackagesNoticesAsync(IPackageRepository repository, CancellationToken token)
        {
           var libraries = await repository.Storage.GetAllLibrariesAsync(token);
           var result = new List<Package>(libraries.Count);

           var sorted = libraries
               .OrderBy(i => i.Name)
               .ThenBy(i => i.Version)
               .ThenBy(i => i.SourceCode);

           foreach (var id in sorted)
           {
               var package = await repository.LoadPackageAsync(id, token);
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
