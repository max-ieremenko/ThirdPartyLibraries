namespace ThirdPartyLibraries.Domain;

public readonly struct PackageSource
{
    public PackageSource(string text, Uri downloadUrl)
    {
        Text = text;
        DownloadUrl = downloadUrl;
    }

    public string Text { get; }
    
    public Uri DownloadUrl { get; }
}