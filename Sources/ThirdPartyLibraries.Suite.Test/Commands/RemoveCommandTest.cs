using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Internal;

namespace ThirdPartyLibraries.Suite.Commands;

[TestFixture]
public class RemoveCommandTest
{
    private const string AppName = "App";

    private List<string> _logs;
    private Mock<IStorage> _storage;
    private Mock<IPackageRepository> _packageRepository;
    private IList<LibraryId> _libraries;
    private RemoveCommand _sut;
    private IServiceProvider _serviceProvider;

    [SetUp]
    public void BeforeEachTest()
    {
        _logs = new List<string>();

        var logger = new Mock<ILogger>(MockBehavior.Strict);
        logger
            .Setup(l => l.Indent())
            .Returns((IDisposable)null);
        logger
            .Setup(l => l.Info(It.IsAny<string>()))
            .Callback<string>(_logs.Add);

        _libraries = new List<LibraryId>();

        _storage = new Mock<IStorage>(MockBehavior.Strict);
        _storage
            .SetupGet(s => s.ConnectionString)
            .Returns("the path");
        _storage
            .Setup(s => s.GetAllLibrariesAsync(CancellationToken.None))
            .ReturnsAsync(_libraries);

        _packageRepository = new Mock<IPackageRepository>(MockBehavior.Strict);
        _packageRepository
            .SetupGet(r => r.Storage)
            .Returns(_storage.Object);
        
        var services = new ServiceCollection();
        services.AddSingleton(_packageRepository.Object);
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

        _packageRepository
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[0], AppName, CancellationToken.None))
            .ReturnsAsync(PackageRemoveResult.None);
        _packageRepository
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[1], AppName, CancellationToken.None))
            .ReturnsAsync(PackageRemoveResult.Removed);

        await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

        _packageRepository.VerifyAll();
        _logs.Last().ShouldBe("Updated 1; removed 0");
    }

    [Test]
    public async Task RemoveLibrary()
    {
        _libraries.Add(new LibraryId("source1", "name1", "version1"));
        _libraries.Add(new LibraryId("source2", "name2", "version2"));

        _packageRepository
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[0], AppName, CancellationToken.None))
            .ReturnsAsync(PackageRemoveResult.None);
        _packageRepository
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[1], AppName, CancellationToken.None))
            .ReturnsAsync(PackageRemoveResult.RemovedNoRefs);

        await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

        _packageRepository.VerifyAll();
        _logs.Last().ShouldBe("Updated 0; removed 1");
    }

    [Test]
    public async Task RemoveReferenceAndLibrary()
    {
        _sut.AppNames.Add("app2");

        _libraries.Add(new LibraryId("source1", "name1", "version1"));

        _packageRepository
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[0], AppName, CancellationToken.None))
            .ReturnsAsync(PackageRemoveResult.Removed);
        _packageRepository
            .Setup(r => r.RemoveFromApplicationAsync(_libraries[0], "app2", CancellationToken.None))
            .ReturnsAsync(PackageRemoveResult.RemovedNoRefs);

        await _sut.ExecuteAsync(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

        _packageRepository.VerifyAll();
        _logs.Last().ShouldBe("Updated 0; removed 1");
    }
}