using System.Collections.Generic;
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
        private const string OutputFileName = "ThirdPartyNotices.txt";

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
            var packages = await repository.LoadAllPackagesNoticesAsync(token);

            var rootContext = new ThirdPartyNoticesContext();

            foreach (var package in packages.Where(i => !i.LicenseCode.IsNullOrEmpty()))
            {
                var license = await state.GetLicensesAsync(package.LicenseCode, token);

                var packageContext = new ThirdPartyNoticesPackageContext
                {
                    Name = package.Name,
                    License = license,
                    Copyright = package.Copyright,
                    HRef = package.HRef,
                    Author = package.Author
                };

                rootContext.Packages.Add(packageContext);
                license.Packages.Add(packageContext);
            }

            var template = await repository.Storage.GetOrCreateThirdPartyNoticesTemplateAsync(token);
            var fileName = Path.Combine(To, OutputFileName);
            using (var file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                DotLiquidTemplate.RenderTo(file, template, rootContext);
            }

            return true;
        }
    }
}
