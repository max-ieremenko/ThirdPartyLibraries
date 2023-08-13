using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Configuration;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries;

[TestFixture]
public class HelpCommandTest
{
    private HelpCommand _sut = null!;
    private IServiceProvider _serviceProvider = null!;
    private string? _loggerOutput;

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

        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        serviceProvider
            .Setup(p => p.GetService(typeof(ILogger)))
            .Returns(logger.Object);
        _serviceProvider = serviceProvider.Object;

        _sut = new HelpCommand(null);
    }

    [Test]
    public async Task GenericHelp()
    {
        await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

        _loggerOutput!.ShouldContain("<command> [options]...");
    }

    [Test]
    [TestCase(CommandOptions.CommandUpdate)]
    [TestCase(CommandOptions.CommandGenerate)]
    [TestCase(CommandOptions.CommandRefresh)]
    [TestCase(CommandOptions.CommandValidate)]
    public async Task CommandHelp(string command)
    {
        _sut.Command = command;

        await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

        _loggerOutput!.ShouldContain($"{command} [options]...");
    }
}