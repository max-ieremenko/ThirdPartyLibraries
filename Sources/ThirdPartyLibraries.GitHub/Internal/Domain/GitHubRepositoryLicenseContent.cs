﻿using System.Text.Json.Serialization;

namespace ThirdPartyLibraries.GitHub.Internal.Domain;

internal sealed class GitHubRepositoryLicenseContent
{
    public string? Name { get; set; }

    [JsonPropertyName("spdx_id")]
    public string? SpdxId { get; set; }
}