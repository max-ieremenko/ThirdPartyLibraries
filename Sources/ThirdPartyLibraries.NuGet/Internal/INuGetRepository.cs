﻿namespace ThirdPartyLibraries.NuGet.Internal;

internal interface INuGetRepository
{
    Task<byte[]?> TryGetPackageFromCacheAsync(string packageName, string version, List<Uri> sources, CancellationToken token);

    Task<byte[]?> TryDownloadPackageAsync(string packageName, string version, CancellationToken token);

    string? ResolvePackageSource(string packageName, string version, List<Uri> sources);
}