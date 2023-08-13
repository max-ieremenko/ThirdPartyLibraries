namespace ThirdPartyLibraries.NuGet.Internal;

// https://semver.org/spec/v2.0.0.html
internal readonly ref struct SemanticVersion
{
    public SemanticVersion(string value)
    {
        var index = value.IndexOf('+');
        if (index < 0)
        {
            Version = value;
            Build = null;
        }
        else
        {
            Version = value.Substring(0, index);
            Build = value.Substring(index + 1);
        }
    }

    public string Version { get; }

    public string? Build { get; }
}