namespace ThirdPartyLibraries.NuGet.Internal;

internal readonly struct NuGetMetadata
{
    public NuGetMetadata(int version, string source)
    {
        Version = version;
        Source = source;
    }

    public int Version { get; }

    public string Source { get; }
}