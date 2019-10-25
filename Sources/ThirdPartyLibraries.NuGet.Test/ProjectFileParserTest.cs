using System.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using Shouldly;

namespace ThirdPartyLibraries.NuGet
{
    [TestFixture]
    public class ProjectFileParserTest
    {
        [Test]
        public void ParseCsProj()
        {
            var content = GetFileContent("ProjectFileParserTest.csproj.xml");
            var actual = ProjectFileParser.ParseCsProjFile(content).ToList();

            actual.Count.ShouldBe(2);
            actual[0].Name.ShouldBe("Microsoft.NET.Test.Sdk");
            actual[0].Version.ShouldBe("16.2.0");
            actual[1].Name.ShouldBe("StyleCop.Analyzers");
            actual[1].Version.ShouldBe("1.1.118");
        }

        [Test]
        public void GetTargetFramework()
        {
            var content = GetFileContent("ProjectFileParserTest.csproj.xml");
            var actual = ProjectFileParser.GetTargetFrameworks(content).ToList();

            actual.ShouldBe(new[] { "netcoreapp3.0" });
        }

        [Test]
        public void GetTargetFrameworks()
        {
            var content = GetFileContent("ProjectFileParserTest.Targets.csproj.xml");
            var actual = ProjectFileParser.GetTargetFrameworks(content).ToList();

            actual.ShouldBe(new[] { "net452", "netcoreapp2.2" });
        }

        private XPathNavigator GetFileContent(string name)
        {
            using var stream = TempFile.OpenResource(GetType(), name);
            return new XPathDocument(stream).CreateNavigator();
        }
    }
}
