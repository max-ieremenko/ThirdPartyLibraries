using System.Text.Json;

namespace ThirdPartyLibraries.NuGet.Internal;

internal static class NuGetMetadataParser
{
    public static bool TryGetSource(string fileName, [NotNullWhen(true)] out Uri? source)
    {
        using (var stream = File.OpenRead(fileName))
        {
            return TryGetSource(stream, out source);
        }
    }

    public static bool TryGetSource(Stream stream, [NotNullWhen(true)] out Uri? source)
    {
        source = null;
        string? path = null;
        using (var content = JsonDocument.Parse(stream))
        {
            if (content.RootElement.TryGetProperty("source", out var value) && value.ValueKind == JsonValueKind.String)
            {
                path = value.GetString();
            }
        }
        
        return !string.IsNullOrEmpty(path) && Uri.TryCreate(path, UriKind.Absolute, out source);
    }
}