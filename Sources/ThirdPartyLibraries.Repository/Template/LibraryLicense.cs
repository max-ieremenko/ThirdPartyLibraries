using Newtonsoft.Json;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class LibraryLicense
{
    public string Subject { get; set; } = null!;

    public string? Code { get; set; }

    public string? HRef { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Description { get; set; }

    public LibraryLicense Clone() => (LibraryLicense)MemberwiseClone();
}