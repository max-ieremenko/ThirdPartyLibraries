﻿using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using Shouldly;
using ThirdPartyLibraries.Repository;
using Unity;

namespace ThirdPartyLibraries.Suite.Internal
{
    [TestFixture]
    public class SourceCodeParserTest
    {
        private Mock<ISourceCodeReferenceProvider> _referenceProvider;
        private SourceCodeParser _sut;

        [SetUp]
        public void BeforeEachTest()
        {
            var container = new UnityContainer();

            _referenceProvider = new Mock<ISourceCodeReferenceProvider>(MockBehavior.Strict);
            container.RegisterInstance("some provider", _referenceProvider.Object);

            _sut = new SourceCodeParser(container);
        }

        [Test]
        public void GetReferences()
        {
            var expected = new LibraryReference(
                new LibraryId("source", "name", "version"),
                Array.Empty<string>(),
                Array.Empty<LibraryId>(),
                true);

            _referenceProvider
                .Setup(r => r.GetReferencesFrom("some path"))
                .Returns(new[] { expected });

            var actual = _sut.GetReferences(new[] { "some path" });

            actual.ShouldBe(new[] { expected });
        }

        [Test]
        public void DistinctCombineTargetFrameworks()
        {
            var id = new LibraryId("source", "name", "version");
            var references = new[]
            {
                new LibraryReference(
                    id,
                    new[] { "f1" },
                    new[] { new LibraryId("source", "d", "1") },
                    false),
                new LibraryReference(
                    id,
                    new[] { "f2" },
                    new[] { new LibraryId("source", "d", "2") },
                    false)
            };

            var actual = _sut.Distinct(references).ToList();

            actual.Count.ShouldBe(1);

            actual[0].Id.ShouldBe(id);
            actual[0].TargetFrameworks.ShouldBe(new[] { "f1", "f2" }, true);

            actual[0].Dependencies.Count.ShouldBe(2);
            actual[0].Dependencies[0].Name.ShouldBe("d");
            actual[0].Dependencies[0].Version.ShouldBe("1");
            actual[0].Dependencies[1].Name.ShouldBe("d");
            actual[0].Dependencies[1].Version.ShouldBe("2");
        }

        [Test]
        public void DistinctNoCombinations()
        {
            var id1 = new LibraryId("source", "name", "version1");
            var id2 = new LibraryId("source", "name", "version2");
            var references = new[]
            {
                new LibraryReference(id1, new[] { "f1" }, Array.Empty<LibraryId>(), false),
                new LibraryReference(id2, new[] { "f2" }, Array.Empty<LibraryId>(), false)
            };

            var actual = _sut.Distinct(references).ToList();

            actual.Count.ShouldBe(2);

            actual[0].Id.ShouldBe(id1);
            actual[0].TargetFrameworks.ShouldBe(new[] { "f1" });

            actual[1].Id.ShouldBe(id2);
            actual[1].TargetFrameworks.ShouldBe(new[] { "f2" });
        }
    }
}
