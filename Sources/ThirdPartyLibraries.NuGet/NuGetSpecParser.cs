using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet
{
    internal static class NuGetSpecParser
    {
        internal const string NoGroupTargetFramework = "";
        internal const string FallbackGroupTargetFramework = "fallback";

        public static NuGetSpec FromStream(Stream stream)
        {
            stream.AssertNotNull(nameof(stream));

            var doc = new XPathDocument(stream);
            
            var navigator = doc.CreateNavigator();
            navigator.MoveToFollowing(XPathNodeType.Element);
            var namespaceUri = navigator.GetNamespacesInScope(XmlNamespaceScope.All)[string.Empty];

            var ns = new XmlNamespaceManager(navigator.NameTable);
            ns.AddNamespace("n", namespaceUri);

            var spec = ParseSpec(doc.CreateNavigator(), ns);
            spec.PackageHRef = "https://" + KnownHosts.NuGetOrg + "/packages/" + HttpUtility.UrlEncode(spec.Id) + "/" + HttpUtility.UrlEncode(spec.Version);
            spec.Version = new SemanticVersion(spec.Version).Version;

            return spec;
        }

        // https://docs.microsoft.com/en-us/nuget/reference/nuspec#dependencies-element
        public static IEnumerable<NuGetPackageId> ExtractDependencies(
            IDictionary<string, NuGetPackageId[]> dependenciesByTargetFramework,
            string targetFramework)
        {
            dependenciesByTargetFramework.TryGetValue(NoGroupTargetFramework, out var noGroup);

            if (!dependenciesByTargetFramework.TryGetValue(targetFramework, out var targetGroup))
            {
                dependenciesByTargetFramework.TryGetValue(FallbackGroupTargetFramework, out targetGroup);
            }

            return (noGroup ?? Enumerable.Empty<NuGetPackageId>()).Concat(targetGroup ?? Enumerable.Empty<NuGetPackageId>());
        }

        private static NuGetSpec ParseSpec(XPathNavigator navigator, XmlNamespaceManager ns)
        {
            var spec = new NuGetSpec
            {
                Id = navigator.SelectSingleNode("n:package/n:metadata/n:id", ns).Value,
                Version = navigator.SelectSingleNode("n:package/n:metadata/n:version", ns).Value,
                LicenseUrl = navigator.SelectSingleNode("n:package/n:metadata/n:licenseUrl", ns)?.Value,
                ProjectUrl = navigator.SelectSingleNode("n:package/n:metadata/n:projectUrl", ns)?.Value,
                Description = navigator.SelectSingleNode("n:package/n:metadata/n:description", ns)?.Value,
                Authors = navigator.SelectSingleNode("n:package/n:metadata/n:authors", ns)?.Value,
                Copyright = navigator.SelectSingleNode("n:package/n:metadata/n:copyright", ns)?.Value,
                Repository = ParseSpecRepository(navigator, ns),
                License = ParseSpecLicense(navigator, ns)
            };

            if (Version.TryParse(spec.Version, out var version) && version.Build < 0)
            {
                // minimum 3 octets in the package version
                spec.Version += ".0";
            }

            return spec;
        }

        private static NuGetSpecLicense ParseSpecLicense(XPathNavigator navigator, XmlNamespaceManager ns)
        {
            var node = navigator.SelectSingleNode("n:package/n:metadata/n:license", ns);
            if (node == null)
            {
                return null;
            }

            return new NuGetSpecLicense
            {
                Value = node.Value,
                Type = node.GetAttribute("type", string.Empty)
            };
        }

        private static NuGetSpecRepository ParseSpecRepository(XPathNavigator navigator, XmlNamespaceManager ns)
        {
            var node = navigator.SelectSingleNode("n:package/n:metadata/n:repository", ns);
            if (node == null)
            {
                return null;
            }

            return new NuGetSpecRepository
            {
                Url = node.GetAttribute("url", string.Empty),
                Type = node.GetAttribute("type", string.Empty)
            };
        }
    }
}
