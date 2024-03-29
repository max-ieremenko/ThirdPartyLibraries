﻿using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Remove;

internal interface IPackageRemover
{
    Task<List<LibraryId>> GetAllLibrariesAsync(CancellationToken token);

    Task<RemoveResult> RemoveFromApplicationAsync(LibraryId id, string appName, CancellationToken token);
}