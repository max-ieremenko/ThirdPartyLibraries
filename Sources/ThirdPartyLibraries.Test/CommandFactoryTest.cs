using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite;
using ThirdPartyLibraries.Suite.Commands;
using Unity;

namespace ThirdPartyLibraries
{
    [TestFixture]
    public class CommandFactoryTest
    {
        private IUnityContainer _container;
        private CommandLine _line;
        private CommandFactory _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            _container = new UnityContainer();
            _line = new CommandLine();

            var logger = new Mock<ILogger>(MockBehavior.Strict);
            _container.RegisterInstance(logger.Object);

            _sut = new CommandFactory { Container = _container };
        }

        [Test]
        public async Task EmptyCommandLine()
        {
            var actual = await _sut.CreateAsync(_line, CancellationToken.None);

            var help = actual.ShouldBeOfType<HelpCommand>();
            help.Command.ShouldBeNull();
        }

        [Test]
        public void UnknownCommand()
        {
            _line.Command = "unknown command";

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(_line, CancellationToken.None));

            ex.Message.ShouldContain("unknown command");
        }

        [Test]
        public async Task HelpForUpdateCommand()
        {
            _line.Command = CommandFactory.CommandUpdate;
            _line.Options.Add(new CommandOption(CommandFactory.OptionHelp));

            var help = (await _sut.CreateAsync(_line, CancellationToken.None)).ShouldBeOfType<HelpCommand>();

            help.Command.ShouldBe(CommandFactory.CommandUpdate);
        }

        [Test]
        public async Task CreateUpdateCommand()
        {
            _line.Command = CommandFactory.CommandUpdate;
            _line.Options.Add(new CommandOption(CommandFactory.OptionAppName, "app name"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionSource, @"c:\folder1"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionSource, "folder2"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionRepository, "repository"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionGitHubToken, "token-value"));

            var chain = (await _sut.CreateAsync(_line, CancellationToken.None)).ShouldBeOfType<CommandChain>();

            chain.Chain.Length.ShouldBe(2);

            var command = chain.Chain[0].ShouldBeOfType<UpdateCommand>();
            command.AppName.ShouldBe("app name");
            
            command.Sources.Count.ShouldBe(2);
            command.Sources[0].ShouldBe(@"c:\folder1");
            Path.IsPathRooted(command.Sources[1]).ShouldBeTrue();
            command.Sources[1].ShouldEndWith("folder2");

            ValidateStorage(@"\repository");

            var section = _container.Resolve<IConfigurationManager>().GetSection<Dictionary<string, string>>("github.com");
            section.Keys.ShouldContain("personalAccessToken");
            section["personalAccessToken"].ShouldBe("token-value");
        }

        [Test]
        public async Task CreateRefreshCommand()
        {
            _line.Command = CommandFactory.CommandRefresh;
            _line.Options.Add(new CommandOption(CommandFactory.OptionRepository, "repository"));

            (await _sut.CreateAsync(_line, CancellationToken.None)).ShouldBeOfType<RefreshCommand>();

            ValidateStorage(@"\repository");
        }

        [Test]
        public async Task CreateValidateCommand()
        {
            _line.Command = CommandFactory.CommandValidate;
            _line.Options.Add(new CommandOption(CommandFactory.OptionAppName, "app name"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionSource, @"c:\folder1"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionSource, "folder2"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionRepository, "repository"));

            var command = (await _sut.CreateAsync(_line, CancellationToken.None)).ShouldBeOfType<ValidateCommand>();
            
            command.AppName.ShouldBe("app name");

            command.Sources.Count.ShouldBe(2);
            command.Sources[0].ShouldBe(@"c:\folder1");
            Path.IsPathRooted(command.Sources[1]).ShouldBeTrue();
            command.Sources[1].ShouldEndWith("folder2");

            ValidateStorage(@"\repository");
        }

        [Test]
        public async Task CreateGenerateCommand()
        {
            _line.Command = CommandFactory.CommandGenerate;
            _line.Options.Add(new CommandOption(CommandFactory.OptionAppName, "app name 1"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionAppName, "app name 2"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionRepository, "repository"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionTo, @"c:\folder1"));

            var command = (await _sut.CreateAsync(_line, CancellationToken.None)).ShouldBeOfType<GenerateCommand>();

            command.AppNames.ShouldBe(new[] { "app name 1", "app name 2" });
            command.To.ShouldBe(@"c:\folder1");

            ValidateStorage(@"\repository");
        }

        private void ValidateStorage(string path)
        {
            _container.IsRegistered<IStorage>().ShouldBeTrue();
            
            var storage = _container.Resolve<IStorage>();

            Path.IsPathRooted(storage.ConnectionString).ShouldBeTrue();
            storage.ConnectionString.ShouldEndWith(path);
        }
    }
}
