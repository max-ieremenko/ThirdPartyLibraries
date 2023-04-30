using System.Collections.Generic;
using Newtonsoft.Json;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class LibraryIndexJson
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Source { get; set; }

    public LicenseConclusion License { get; } = new LicenseConclusion();

    public IList<Application> UsedBy { get; } = new List<Application>();

    public IList<LibraryLicense> Licenses { get; } = new List<LibraryLicense>();
}