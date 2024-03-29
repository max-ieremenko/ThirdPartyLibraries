﻿namespace ThirdPartyLibraries.Npm.Internal;

internal interface INpmRegistry
{
    Task<byte[]?> DownloadPackageAsync(string packageName, string version, CancellationToken token);
}