using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries
{
    [TestFixture]
    public class HelpCommandTest
    {
        private HelpCommand _sut;
        private string _loggerOutput;

        [SetUp]
        public void BeforeEachTest()
        {
            var logger = new Mock<ILogger>(MockBehavior.Strict);
            logger
                .Setup(l => l.Info(It.IsNotNull<string>()))
                .Callback<string>(m =>
                {
                    Console.WriteLine(m);
                    _loggerOutput = m;
                });

            _sut = new HelpCommand { Logger = logger.Object };
        }

        [Test]
        public async Task GenericHelp()
        {
            await _sut.ExecuteAsync(CancellationToken.None);

            _loggerOutput.ShouldContain("<command> [options]...");
        }

        [Test]
        [TestCase(CommandFactory.CommandUpdate)]
        [TestCase(CommandFactory.CommandGenerate)]
        [TestCase(CommandFactory.CommandRefresh)]
        [TestCase(CommandFactory.CommandValidate)]
        public async Task CommandHelp(string command)
        {
            _sut.Command = command;

            await _sut.ExecuteAsync(CancellationToken.None);

            _loggerOutput.ShouldContain("{0} [options]...".FormatWith(command));
        }
    }
}
