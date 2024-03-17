using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Shared;

internal static class LibraryIdExtensions
{
    public const string CustomPackageSources = "custom";

    public static bool IsCustomSource(this LibraryId id) => CustomPackageSources.Equals(id.SourceCode, StringComparison.OrdinalIgnoreCase);
}