using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository;
using ThirdPartyLibraries.Shared;
using ThirdPartyLibraries.Suite.Shared;
using ThirdPartyLibraries.Suite.Validate.Internal;

namespace ThirdPartyLibraries.Suite.Validate;

[TestFixture]
public class ValidateCommandTest
{
    private List<IPackageReference> _references = null!;
    private Mock<IPackageValidator> _packageValidator = null!;
    private Mock<IValidationState> _state = null!;
    private IServiceProvider _serviceProvider = null!;
    private ValidateCommand _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        var logger = new Mock<ILogger>(MockBehavior.Loose);

        _references = new List<IPackageReference>();

        var storage = new Mock<IStorage>(MockBehavior.Strict);
        storage
            .SetupGet(s => s.ConnectionString)
            .Returns("some path");

        var parser = new Mock<ISourceCodeParser>(MockBehavior.Strict);
        parser
            .Setup(p => p.GetReferences(It.IsNotNull<IList<string>>()))
            .Returns<IList<string>>(locations =>
            {
                locations.ShouldBe(new[] { "source1", "source2" });
                return _references;
            });

        _packageValidator = new Mock<IPackageValidator>(MockBehavior.Strict);
        
        _state = new Mock<IValidationState>(MockBehavior.Strict);
        _state
            .Setup(s => s.InitializeAsync(default))
            .Returns(Task.CompletedTask);

        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        serviceProvider
            .Setup(p => p.GetService(typeof(IStorage)))
            .Returns(storage.Object);
        serviceProvider
            .Setup(p => p.GetService(typeof(ISourceCodeParser)))
            .Returns(parser.Object);
        serviceProvider
            .Setup(p => p.GetService(typeof(ILogger)))
            .Returns(logger.Object);
        serviceProvider
            .Setup(p => p.GetService(typeof(IPackageValidator)))
            .Returns(_packageValidator.Object);
        serviceProvider
            .Setup(p => p.GetService(typeof(IValidationState)))
            .Returns(_state.Object);
        _serviceProvider = serviceProvider.Object;

        _sut = new ValidateCommand
        {
            AppName = "App",
            Sources = { "source1", "source2" }
        };
    }

    [Test]
    public async Task Success()
    {
        _state
            .Setup(s => s.GetNotProcessed())
            .Returns(new List<LibraryId>());
        _state
            .Setup(s => s.GetWithError(It.IsAny<ValidationResult>()))
            .Returns((LibraryId[]?)null);

        await _sut.ExecuteAsync(_serviceProvider, default).ConfigureAwait(false);
    }

    [Test]
    public async Task Errors()
    {
        _state
            .Setup(s => s.GetNotProcessed())
            .Returns(new List<LibraryId>());
        _state
            .Setup(s => s.GetWithError(It.IsAny<ValidationResult>()))
            .Returns(new[] { new LibraryId("dummy", "dummy", "dummy") });

        var actual = await Should.ThrowAsync<RepositoryValidationException>(() => _sut.ExecuteAsync(_serviceProvider, default)).ConfigureAwait(false);

        actual.Errors.Length.ShouldBe(7);
    }

    [Test]
    public async Task ValidateReferenceAsync()
    {
        var id = new LibraryId("dummy", "dummy", "dummy");

        _references.Add(PackageReferenceMock.Create(id).Object);

        _packageValidator
            .Setup(v => v.ValidateReferenceAsync(_references[0], "App", default))
            .Returns(Task.FromResult(ValidationResult.IndexNotFound));

        _state
            .Setup(s => s.SetResult(id, ValidationResult.IndexNotFound));
        _state
            .Setup(s => s.GetNotProcessed())
            .Returns(new List<LibraryId>());
        _state
            .Setup(s => s.GetWithError(It.IsAny<ValidationResult>()))
            .Returns((LibraryId[]?)null);

        await _sut.ExecuteAsync(_serviceProvider, default).ConfigureAwait(false);

        _packageValidator.VerifyAll();
        _state.VerifyAll();
    }

    [Test]
    public async Task ValidateLibraryAsync()
    {
        var id = new LibraryId("dummy", "dummy", "dummy");

        _packageValidator
            .Setup(v => v.ValidateLibraryAsync(id, "App", default))
            .Returns(Task.FromResult(ValidationResult.ReferenceNotFound));

        _state
            .Setup(s => s.SetResult(id, ValidationResult.ReferenceNotFound));
        _state
            .Setup(s => s.GetNotProcessed())
            .Returns(new List<LibraryId> { id });
        _state
            .Setup(s => s.GetWithError(It.IsAny<ValidationResult>()))
            .Returns((LibraryId[]?)null);

        await _sut.ExecuteAsync(_serviceProvider, default).ConfigureAwait(false);

        _packageValidator.VerifyAll();
        _state.VerifyAll();
    }
}