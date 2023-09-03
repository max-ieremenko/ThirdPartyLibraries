using System;
using System.Diagnostics.CodeAnalysis;

namespace ThirdPartyLibraries.Domain;

public interface IRepositoryNameParser
{
    bool TryGetRepository(Uri url, [NotNullWhen(true)] out string? owner, [NotNullWhen(true)] out string? name);
}