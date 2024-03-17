using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Shared.Internal;

[TestFixture]
public class SourceCodeParserTest
{
    private Mock<IPackageReferenceProvider> _referenceProvider = null!;
    private SourceCodeParser _sut = null!;

    [SetUp]
    public void BeforeEachTest()
    {
        _referenceProvider = new Mock<IPackageReferenceProvider>(MockBehavior.Strict);

        _sut = new SourceCodeParser(new[] { _referenceProvider.Object });
    }

    [Test]
    public void AddReferencesFrom()
    {
        var expected = PackageReferenceMock.Create(new LibraryId("source", "name", "version"));

        _referenceProvider
            .Setup(r => r.AddReferencesFrom("some path", It.IsNotNull<List<IPackageReference>>(), It.IsNotNull<HashSet<LibraryId>>()))
            .Callback<string, List<IPackageReference>, HashSet<LibraryId>>((_, references, _) =>
            {
                references.Add(expected.Object);
            });

        var actual = _sut.GetReferences(new[] { "some path" });

        actual.ShouldBe(new[] { expected.Object });
    }

    [Test]
    public void AddReferencesFromNotFound()
    {
        var expected = new LibraryId("source", "name", "version");

        _referenceProvider
            .Setup(r => r.AddReferencesFrom("some path", It.IsNotNull<List<IPackageReference>>(), It.IsNotNull<HashSet<LibraryId>>()))
            .Callback<string, List<IPackageReference>, HashSet<LibraryId>>((_, _, notFound) =>
            {
                notFound.Add(expected);
            });

        var ex = Assert.Throws<ReferenceNotFoundException>(() => _sut.GetReferences(new[] { "some path" }));

        ex!.Libraries.ShouldBe(new[] { expected });
    }

    [Test]
    public void DistinctCombine()
    {
        var reference1 = PackageReferenceMock.Create(new LibraryId("source", "name", "version"));
        var reference2 = PackageReferenceMock.Create(new LibraryId("source", "name", "version"));

        var expected = PackageReferenceMock.Create(new LibraryId("source", "name", "version"));
        reference1
            .Setup(r => r.UnionWith(reference2.Object))
            .Returns(expected.Object);

        var actual = SourceCodeParser.Distinct(new List<IPackageReference> { reference1.Object, reference2.Object });

        actual.ShouldBe(new[] { expected.Object });
    }

    [Test]
    public void DistinctNoCombinations()
    {
        var reference1 = PackageReferenceMock.Create(new LibraryId("source1", "name", "version"));
        var reference2 = PackageReferenceMock.Create(new LibraryId("source2", "name", "version"));

        var actual = SourceCodeParser.Distinct(new List<IPackageReference> { reference1.Object, reference2.Object });

        actual.ShouldBe(new[] { reference1.Object, reference2.Object }, ignoreOrder: true);
    }
}