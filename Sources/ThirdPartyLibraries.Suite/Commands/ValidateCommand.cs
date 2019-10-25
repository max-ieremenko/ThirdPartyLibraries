using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Shared;
using Unity;

namespace ThirdPartyLibraries.Suite.Commands
{
    public sealed class ValidateCommand : ICommand
    {
        public ValidateCommand(IUnityContainer container, ILogger logger)
        {
            container.AssertNotNull(nameof(container));
            logger.AssertNotNull(nameof(logger));

            Container = container;
            Logger = logger;
        }

        public IUnityContainer Container { get; }

        public ILogger Logger { get; }

        public string AppName { get; set; }

        public IList<string> Sources { get; } = new List<string>();

        public Task ExecuteAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
