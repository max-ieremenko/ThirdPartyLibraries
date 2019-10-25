using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.NuGet
{
    [TestFixture]
    public class ProjectAssetsParserTest
    {
        private ProjectAssetsParser _sut;

        [OneTimeSetUp]
        public void BeforeAllTests()
        {
            using (var stream = TempFile.OpenResource(GetType(), "ProjectAssetsParserTest.project.assets.json"))
            {
                _sut = ProjectAssetsParser.FromStream(stream);
            }
        }

        [Test]
        public void GetTargetFrameworks()
        {
            var actual = _sut.GetTargetFrameworks();

            actual.ShouldBe(new[] { "net452", "netcoreapp2.2" }, ignoreOrder: true);
        }

        [Test]
        public void GetNet452References()
        {
            var actual = _sut.GetReferences("net452").ToList();

            actual.Count.ShouldBe(1);

            actual[0].Package.Name.ShouldBe("StyleCop.Analyzers");
            actual[0].Package.Version.ShouldBe("1.1.118");
        }

        [Test]
        public void GetNetCore22References()
        {
            var actual = _sut.GetReferences("netcoreapp2.2").ToList();

            actual.Count.ShouldBe(11);
            actual[2].Package.Name.ShouldBe("System.Configuration.ConfigurationManager");
            actual[2].Package.Version.ShouldBe("4.5.0");

            var dependencies = actual[2].Dependencies;
            dependencies.Count.ShouldBe(2);

            dependencies[0].Name.ShouldBe("System.Security.Cryptography.ProtectedData");
            dependencies[0].Version.ShouldBe("4.5.0");

            dependencies[1].Name.ShouldBe("System.Security.Permissions");
            dependencies[1].Version.ShouldBe("4.5.0");
        }

        [Test]
        public void GetProjectName()
        {
            _sut.GetProjectName().ShouldBe("Company.Name.Project");
        }
    }
}
