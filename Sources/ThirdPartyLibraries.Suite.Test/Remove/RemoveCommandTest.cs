using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Suite.Remove;

[TestFixture]
public class RemoveCommandTest
{
    private const string AppName = "App";

    private List<string> _logs = null!;
    private Mock<IStorage> _storage = null!;
    private Mock<IPackageRemover> _packageRemover = null!;
    private List<LibraryId> _libraries = null!;
    private RemoveCommand _sut = null!;
    private IServiceProvider _serviceProvider = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _logs = new List<string>();

        var logger = new Mock<ILogger>(MockBehavior.Strict);
        logger
            .Setup(l => l.Indent())
            .Returns((IDisposable)null!);
        logger
            .Setup(l => l.Info(It.IsAny<string>()))
            .Callback<string>(_logs.Add);

        _libraries = new List<LibraryId>();

        _storage = new Mock<IStorage>(MockBehavior.Strict);
        _storage
            .SetupGet(s => s.ConnectionString)
            .Returns("the path");

        _packageRemover = new Mock<IPackageRemover>(MockBehavior.Strict);
        _packageRemover
            .Setup(r => r.GetAllLibrariesAsync(default))
            .ReturnsAsync(_libraries);

        var services = new ServiceCollection();
        services.AddSingleton(_storage.Object);
        services.AddSingleton(_packageRemover.Object);
        services.AddSingleton(logger.Object);
        _serviceProvider = services.BuildServiceProvider();

        _sut = new RemoveCommand
        {
            AppNames = { AppName }
        };
    }

    [Test]
    public async Task RemoveReference()
    {
        _libraries.Add(new LibraryId("source1", "name1", "version1"));
        _libraries.Add(new LibraryId("source2", "name2", "version2"));

        _packageRemover
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[0], AppName, default))
            .ReturnsAsync(RemoveResult.None);
        _packageRemover
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[1], AppName, default))
            .ReturnsAsync(RemoveResult.Updated);

        await _sut.ExecuteAsync(_serviceProvider, default).ConfigureAwait(false);

        _packageRemover.VerifyAll();
        _logs.Last().ShouldBe("Updated 1; removed 0; unchanged 1");
    }

    [Test]
    public async Task RemoveLibrary()
    {
        _libraries.Add(new LibraryId("source1", "name1", "version1"));
        _libraries.Add(new LibraryId("source2", "name2", "version2"));

        _packageRemover
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[0], AppName, default))
            .ReturnsAsync(RemoveResult.None);
        _packageRemover
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[1], AppName, default))
            .ReturnsAsync(RemoveResult.Deleted);

        await _sut.ExecuteAsync(_serviceProvider, default).ConfigureAwait(false);

        _packageRemover.VerifyAll();
        _logs.Last().ShouldBe("Updated 0; removed 1; unchanged 1");
    }

    [Test]
    public async Task RemoveReferenceAndLibrary()
    {
        _sut.AppNames.Add("app2");

        _libraries.Add(new LibraryId("source1", "name1", "version1"));

        _packageRemover
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[0], AppName, default))
            .ReturnsAsync(RemoveResult.Updated);
        _packageRemover
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[0], "app2", default))
            .ReturnsAsync(RemoveResult.Deleted);

        await _sut.ExecuteAsync(_serviceProvider, default).ConfigureAwait(false);

        _packageRemover.VerifyAll();
        _logs.Last().ShouldBe("Updated 0; removed 1; unchanged 0");
    }
}