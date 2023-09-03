﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Validate.Internal;

internal interface IValidationState
{
    Task InitializeAsync(CancellationToken token);

    void SetResult(LibraryId id, ValidationResult result);

    List<LibraryId> GetNotProcessed();

    LibraryId[]? GetWithError(ValidationResult error);
}