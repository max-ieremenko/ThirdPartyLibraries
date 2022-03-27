﻿using System;
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

        [SetUp]
        public void BeforeEachTest()
        {
            _line = new CommandLine();
        }

        [Test]
        public void EmptyCommandLine()
        {
            var actual = CommandFactory.Create(_line, out var repository);

            repository.ShouldBeNull();
            var help = actual.ShouldBeOfType<HelpCommand>();
            help.Command.ShouldBeNull();
        }

        [Test]
        public void UnknownCommand()
        {
            _line.Command = "unknown command";

            var ex = Assert.Throws<InvalidOperationException>(() => CommandFactory.Create(_line, out _));

            ex.Message.ShouldContain("unknown command");
        }

        [Test]
        public void HelpForUpdateCommand()
        {
            _line.Command = CommandFactory.CommandUpdate;
            _line.Options.Add(new CommandOption(CommandFactory.OptionHelp));

            var help = CommandFactory.Create(_line, out var repository).ShouldBeOfType<HelpCommand>();

            repository.ShouldBeNull();
            help.Command.ShouldBe(CommandFactory.CommandUpdate);
        }

        [Test]
        public void CreateUpdateCommand()
        {
            _line.Command = CommandFactory.CommandUpdate;
            _line.Options.Add(new CommandOption(CommandFactory.OptionAppName, "app name"));

            var source = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"c:\folder1" : "/folder1";
            _line.Options.Add(new CommandOption(CommandFactory.OptionSource, source));

            _line.Options.Add(new CommandOption(CommandFactory.OptionSource, "folder2"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionRepository, "repository"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionGitHubToken, "token-value"));

            var chain = CommandFactory.Create(_line, out var repository).ShouldBeOfType<CommandChain>();

            chain.Chain.Length.ShouldBe(2);

            var command = chain.Chain[0].ShouldBeOfType<UpdateCommand>();
            command.AppName.ShouldBe("app name");
            
            command.Sources.Count.ShouldBe(2);
            command.Sources[0].ShouldBe(source);
            Path.IsPathRooted(command.Sources[1]).ShouldBeTrue();
            command.Sources[1].ShouldEndWith("folder2");

            repository.ShouldBe("repository");
        }

        [Test]
        public void CreateRefreshCommand()
        {
            _line.Command = CommandFactory.CommandRefresh;
            _line.Options.Add(new CommandOption(CommandFactory.OptionRepository, "repository"));

            CommandFactory.Create(_line, out var repository).ShouldBeOfType<RefreshCommand>();

            repository.ShouldBe("repository");
        }

        [Test]
        public void CreateValidateCommand()
        {
            _line.Command = CommandFactory.CommandValidate;
            _line.Options.Add(new CommandOption(CommandFactory.OptionAppName, "app name"));

            var source = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"c:\folder1" : "/folder1";
            _line.Options.Add(new CommandOption(CommandFactory.OptionSource, source));

            _line.Options.Add(new CommandOption(CommandFactory.OptionSource, "folder2"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionRepository, "repository"));

            var command = CommandFactory.Create(_line, out var repository).ShouldBeOfType<ValidateCommand>();

            command.AppName.ShouldBe("app name");

            command.Sources.Count.ShouldBe(2);
            command.Sources[0].ShouldBe(source);
            Path.IsPathRooted(command.Sources[1]).ShouldBeTrue();
            command.Sources[1].ShouldEndWith("folder2");

            repository.ShouldBe("repository");
        }

        [Test]
        public void CreateGenerateCommand()
        {
            _line.Command = CommandFactory.CommandGenerate;
            _line.Options.Add(new CommandOption(CommandFactory.OptionAppName, "app name 1"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionAppName, "app name 2"));
            _line.Options.Add(new CommandOption(CommandFactory.OptionRepository, "repository"));

            var to = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"c:\folder1" : "/folder1";
            _line.Options.Add(new CommandOption(CommandFactory.OptionTo, to));

            var command = CommandFactory.Create(_line, out var repository).ShouldBeOfType<GenerateCommand>();

            command.AppNames.ShouldBe(new[] { "app name 1", "app name 2" });
            command.To.ShouldBe(to);

            repository.ShouldBe("repository");
        }
    }
}
