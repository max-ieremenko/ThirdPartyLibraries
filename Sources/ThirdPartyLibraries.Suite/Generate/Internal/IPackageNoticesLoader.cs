﻿using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal interface IPackageNoticesLoader
{
    Task<PackageNotices?> LoadAsync(LibraryId id, List<string> appNames, CancellationToken token);
}