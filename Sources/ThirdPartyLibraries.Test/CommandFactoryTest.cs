using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Suite.Commands;

namespace ThirdPartyLibraries
{
    [TestFixture]
    public class CommandFactoryTest
    {
        private CommandLine _line;
        private Dictionary<string, string> _configuration;

        [SetUp]
        public void BeforeEachTest()
        {
            _line = new CommandLine();
            _configuration = new Dictionary<string, string>();
        }

        [Test]
        public void EmptyCommandLine()
        {
            var actual = CommandFactory.Create(_line, _configuration, out var repository);

            repository.ShouldBeNull();
            var help = actual.ShouldBeOfType<HelpCommand>();
            help.Command.ShouldBeNull();

            _configuration.ShouldBeEmpty();
        }

        [Test]
        public void UnknownCommand()
        {
            _line.Command = "unknown command";

            var ex = Assert.Throws<InvalidOperationException>(() => CommandFactory.Create(_line, _configuration, out _));

            ex.Message.ShouldContain("unknown command");
        }

        [Test]
        public void HelpForUpdateCommand()
        {
            _line.Command = CommandOptions.CommandUpdate;
            _line.Options.Add(new CommandOption(CommandOptions.OptionHelp));

            var help = CommandFactory.Create(_line, _configuration, out var repository).ShouldBeOfType<HelpCommand>();

            repository.ShouldBeNull();
            help.Command.ShouldBe(CommandOptions.CommandUpdate);

            _configuration.ShouldBeEmpty();
        }

        [Test]
        public void CreateUpdateCommand()
        {
            _line.Command = CommandOptions.CommandUpdate;
            _line.Options.Add(new CommandOption(CommandOptions.OptionAppName, "app name"));

            var source = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"c:\folder1" : "/folder1";
            _line.Options.Add(new CommandOption(CommandOptions.OptionSource, source));

            _line.Options.Add(new CommandOption(CommandOptions.OptionSource, "folder2"));
            _line.Options.Add(new CommandOption(CommandOptions.OptionRepository, "repository"));
            _line.Options.Add(new CommandOption(CommandOptions.OptionGitHubToken, "token-value"));

            var chain = CommandFactory.Create(_line, _configuration, out var repository).ShouldBeOfType<CommandChain>();

            chain.Chain.Length.ShouldBe(2);

            var command = chain.Chain[0].ShouldBeOfType<UpdateCommand>();
            command.AppName.ShouldBe("app name");
            
            command.Sources.Count.ShouldBe(2);
            command.Sources[0].ShouldBe(source);
            Path.IsPathRooted(command.Sources[1]).ShouldBeTrue();
            command.Sources[1].ShouldEndWith("folder2");

            repository.ShouldBe("repository");

            _configuration.Keys.ShouldBe(new[] { CommandOptions.OptionGitHubToken });
            _configuration[CommandOptions.OptionGitHubToken].ShouldBe("token-value");
        }

        [Test]
        public void CreateRefreshCommand()
        {
            _line.Command = CommandOptions.CommandRefresh;
            _line.Options.Add(new CommandOption(CommandOptions.OptionRepository, "repository"));

            CommandFactory.Create(_line, _configuration, out var repository).ShouldBeOfType<RefreshCommand>();

            repository.ShouldBe("repository");

            _configuration.ShouldBeEmpty();
        }

        [Test]
        public void CreateValidateCommand()
        {
            _line.Command = CommandOptions.CommandValidate;
            _line.Options.Add(new CommandOption(CommandOptions.OptionAppName, "app name"));

            var source = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"c:\folder1" : "/folder1";
            _line.Options.Add(new CommandOption(CommandOptions.OptionSource, source));

            _line.Options.Add(new CommandOption(CommandOptions.OptionSource, "folder2"));
            _line.Options.Add(new CommandOption(CommandOptions.OptionRepository, "repository"));

            var command = CommandFactory.Create(_line, _configuration, out var repository).ShouldBeOfType<ValidateCommand>();

            command.AppName.ShouldBe("app name");

            command.Sources.Count.ShouldBe(2);
            command.Sources[0].ShouldBe(source);
            Path.IsPathRooted(command.Sources[1]).ShouldBeTrue();
            command.Sources[1].ShouldEndWith("folder2");

            repository.ShouldBe("repository");

            _configuration.ShouldBeEmpty();
        }

        [Test]
        public void CreateGenerateCommand()
        {
            _line.Command = CommandOptions.CommandGenerate;
            _line.Options.Add(new CommandOption(CommandOptions.OptionAppName, "app name 1"));
            _line.Options.Add(new CommandOption(CommandOptions.OptionAppName, "app name 2"));
            _line.Options.Add(new CommandOption(CommandOptions.OptionTitle, "the title"));
            _line.Options.Add(new CommandOption(CommandOptions.OptionRepository, "repository"));

            var to = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"c:\folder1" : "/folder1";
            _line.Options.Add(new CommandOption(CommandOptions.OptionTo, to));

            var command = CommandFactory.Create(_line, _configuration, out var repository).ShouldBeOfType<GenerateCommand>();

            command.AppNames.ShouldBe(new[] { "app name 1", "app name 2" });
            command.Title.ShouldBe("the title");
            command.To.ShouldBe(to);

            repository.ShouldBe("repository");

            _configuration.ShouldBeEmpty();
        }
    }
}
