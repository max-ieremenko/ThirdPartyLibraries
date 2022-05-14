using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.PowerShell.Internal;

[TestFixture]
public class ProgramRunAdapterTest
{
    private CmdLetLoggerMock _logger;
    private ProgramRunAdapter _sut;

    [SetUp]
    public void BeforeEachTest()
    {
        _logger = new CmdLetLoggerMock();
        _sut = new ProgramRunAdapter(_logger);
    }

    [Test]
    public void RunWithInfo()
    {
        _logger.Initialize();
        var task = DoInfo("the message");

        _sut.Wait(task);

        task.IsCompletedSuccessfully.ShouldBeTrue();
        _logger.Output.ShouldBe(new[] { "info: the message" });
    }

    [Test]
    public void RunWithWarn()
    {
        _logger.Initialize();
        var task = DoWarn("the message");

        _sut.Wait(task);

        task.IsCompletedSuccessfully.ShouldBeTrue();
        _logger.Output.ShouldBe(new[] { "warn: the message" });
    }

    [Test]
    public void RunWithException()
    {
        _logger.Initialize();
        var task = DoException(new NotSupportedException("the message"));

        _sut.Wait(task);

        task.IsFaulted.ShouldBeTrue();
        _logger.Output.ShouldBe(new[] { "NotSupportedException: the message" });
    }

    [Test]
    public void RunWithCancellation()
    {
        _logger.Initialize();
        var task = DoException(new TaskCanceledException("the message"));

        _sut.Wait(task);

        task.IsCanceled.ShouldBeTrue();
        _logger.Output.ShouldBe(new[] { "warn: The execution was canceled by the user." });
    }

    private async Task DoInfo(string message)
    {
        await Task.Delay(100).ConfigureAwait(false);
        _sut.OnInfo(message);
    }

    private async Task DoWarn(string message)
    {
        await Task.Delay(100).ConfigureAwait(false);
        _sut.OnWarn(message);
    }

    private async Task DoException(Exception exception)
    {
        await Task.Delay(100).ConfigureAwait(false);
        throw exception;
    }

    private sealed class CmdLetLoggerMock : ICmdLetLogger
    {
        private int _mainThreadId;

        public List<string> Output { get; } = new();

        public void Initialize()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public void Info(string message)
        {
            CheckAccess();
            Output.Add("info: " + message);
        }

        public void Warn(string message)
        {
            CheckAccess();
            Output.Add("warn: " + message);
        }

        public void Error(Exception exception)
        {
            CheckAccess();
            Output.Add(exception.GetType().Name + ": " + exception.Message);
        }

        private void CheckAccess()
        {
            Thread.CurrentThread.ManagedThreadId.ShouldBe(_mainThreadId);
        }
    }
}