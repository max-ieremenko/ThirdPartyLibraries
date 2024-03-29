﻿namespace ThirdPartyLibraries.Domain;

public interface INuGetPackageSourceResolver
{
    bool TryResolve(IPackageSpec spec, Uri packageSource, [NotNullWhen(true)] out PackageSource? source);
}