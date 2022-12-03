using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet;

internal static class NuGetSpecParser
{
    internal const string NoGroupTargetFramework = "";
    internal const string FallbackGroupTargetFramework = "fallback";

    public static NuGetSpec FromStream(Stream stream)
    {
        stream.AssertNotNull(nameof(stream));

        var doc = new XPathDocument(stream);
            
        var metadata = FindMetaData(doc);
        var namespaceUri = metadata.GetNamespacesInScope(XmlNamespaceScope.All)[string.Empty];

        var ns = new XmlNamespaceManager(metadata.NameTable);
        ns.AddNamespace("n", namespaceUri);

        var spec = ParseSpec(FindMetaData(doc), ns);
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

    private static XPathNavigator FindMetaData(XPathDocument doc)
    {
        return doc
            .CreateNavigator()
            .SelectChildren(XPathNodeType.Element)
            .Cast<XPathNavigator>()
            .First(i => "package".Equals(i.Name, StringComparison.Ordinal))
            .SelectChildren(XPathNodeType.Element)
            .Cast<XPathNavigator>()
            .First(i => "metadata".Equals(i.Name, StringComparison.Ordinal));
    }

    private static NuGetSpec ParseSpec(XPathNavigator metadata, XmlNamespaceManager ns)
    {
        var spec = new NuGetSpec
        {
            Id = metadata.SelectSingleNode("n:id", ns).Value,
            Version = metadata.SelectSingleNode("n:version", ns).Value,
            LicenseUrl = metadata.SelectSingleNode("n:licenseUrl", ns)?.Value,
            ProjectUrl = metadata.SelectSingleNode("n:projectUrl", ns)?.Value,
            Description = metadata.SelectSingleNode("n:description", ns)?.Value,
            Authors = metadata.SelectSingleNode("n:authors", ns)?.Value,
            Copyright = metadata.SelectSingleNode("n:copyright", ns)?.Value,
            Repository = ParseSpecRepository(metadata, ns),
            License = ParseSpecLicense(metadata, ns)
        };

        if (Version.TryParse(spec.Version, out var version) && version.Build < 0)
        {
            // minimum 3 octets in the package version
            spec.Version += ".0";
        }

        return spec;
    }

    private static NuGetSpecLicense ParseSpecLicense(XPathNavigator metadata, XmlNamespaceManager ns)
    {
        var node = metadata.SelectSingleNode("n:license", ns);
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

    private static NuGetSpecRepository ParseSpecRepository(XPathNavigator metadata, XmlNamespaceManager ns)
    {
        var node = metadata.SelectSingleNode("n:repository", ns);
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