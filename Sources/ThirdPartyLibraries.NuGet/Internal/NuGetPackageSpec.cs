using System.Xml;
using System.Xml.XPath;
using NuGet.Versioning;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.NuGet.Internal;

internal sealed class NuGetPackageSpec : IPackageSpec
{
    private readonly XPathNavigator _metadata;
    private readonly XmlNamespaceManager _ns;

    private NuGetPackageSpec(XPathNavigator metadata, XmlNamespaceManager ns)
    {
        _metadata = metadata;
        _ns = ns;
    }

    public static NuGetPackageSpec FromStream(Stream stream)
    {
        var doc = new XPathDocument(stream);

        var metadata = doc
            .CreateNavigator()
            .SelectChildren(XPathNodeType.Element)
            .Cast<XPathNavigator>()
            .First(i => "package".Equals(i.Name, StringComparison.Ordinal))
            .SelectChildren(XPathNodeType.Element)
            .Cast<XPathNavigator>()
            .First(i => "metadata".Equals(i.Name, StringComparison.Ordinal));

        var namespaceUri = metadata.GetNamespacesInScope(XmlNamespaceScope.All)[string.Empty];

        var ns = new XmlNamespaceManager(metadata.NameTable);
        ns.AddNamespace("n", namespaceUri);

        return new NuGetPackageSpec(metadata, ns);
    }

    public string GetName()
    {
        return _metadata.SelectSingleNode("n:id", _ns)!.Value;
    }

    public string GetVersion()
    {
        var metadataVersion = _metadata.SelectSingleNode("n:version", _ns)!.Value;
        var version = NuGetVersion.Parse(metadataVersion).ToFullString();
        return new SemanticVersion(version).Version;
    }

    public string? GetDescription()
    {
        return _metadata.SelectSingleNode("n:description", _ns)?.Value;
    }

    public string? GetLicenseType()
    {
        var node = _metadata.SelectSingleNode("n:license", _ns);
        return node?.GetAttribute("type", string.Empty);
    }

    public string? GetLicenseValue()
    {
        return _metadata.SelectSingleNode("n:license", _ns)?.Value;
    }

    public string? GetLicenseUrl()
    {
        return _metadata.SelectSingleNode("n:licenseUrl", _ns)?.Value;
    }

    public string? GetRepositoryUrl()
    {
        var node = _metadata.SelectSingleNode("n:repository", _ns);
        return node?.GetAttribute("url", string.Empty);
    }

    public string? GetProjectUrl()
    {
        return _metadata.SelectSingleNode("n:projectUrl", _ns)?.Value;
    }

    public string? GetCopyright()
    {
        return _metadata.SelectSingleNode("n:copyright", _ns)?.Value;
    }

    public string? GetAuthor()
    {
        return _metadata.SelectSingleNode("n:authors", _ns)?.Value;
    }
}