using System;
using System.Collections.Generic;
using System.Xml.XPath;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet
{
    public static class ProjectFileParser
    {
        public static string[] GetTargetFrameworks(XPathNavigator project)
        {
            project.AssertNotNull(nameof(project));

            var node = project.SelectSingleNode("Project/PropertyGroup/TargetFramework");
            if (node != null)
            {
                return new[] { node.Value };
            }

            node = project.SelectSingleNode("Project/PropertyGroup/TargetFrameworks");
            return node.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static IEnumerable<NuGetPackageId> ParseCsProjFile(XPathNavigator project)
        {
            project.AssertNotNull(nameof(project));

            var nodes = project.Select("Project/ItemGroup/PackageReference[@Include and @Version]");

            foreach (XPathNavigator node in nodes)
            {
                yield return new NuGetPackageId(
                    node.GetAttribute("Include", string.Empty),
                    node.GetAttribute("Version", string.Empty));
            }
        }
    }
}
