﻿using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository.Template;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal interface ILicenseByContentResolver
{
    Task<IdenticalLicenseFile?> TryResolveAsync(LibraryId library, List<LibraryLicense> libraryLicenses, CancellationToken token);
}