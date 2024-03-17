namespace ThirdPartyLibraries.Suite.Shared;

internal static class PackageLicenseApprovalStatus
{
    public const string CodeHasToBeApproved = "HasToBeApproved";
    public const string CodeAutomaticallyApproved = "AutomaticallyApproved";
    public const string CodeApproved = "Approved";

    public static bool IsDefined(string? status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return false;
        }

        return IsApproved(status) || IsAutomaticallyApproved(status) || IsHasToBeApproved(status);
    }

    public static bool IsApproved(string? status) => CodeApproved.Equals(status, StringComparison.OrdinalIgnoreCase);

    public static bool IsAutomaticallyApproved(string? status) => CodeAutomaticallyApproved.Equals(status, StringComparison.OrdinalIgnoreCase);

    public static bool IsHasToBeApproved(string? status) => CodeHasToBeApproved.Equals(status, StringComparison.OrdinalIgnoreCase);
}