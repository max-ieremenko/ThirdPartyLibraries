using ThirdPartyLibraries.Domain;
using ThirdPartyLibraries.Repository.Template;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal static class LibraryIndexJsonChangeTracker
{
    private static readonly Comparison<Application> CompareAppName = (x, y) => StringComparer.Ordinal.Compare(x.Name, y.Name);
    private static readonly Comparison<LibraryDependency> CompareDependency = (x, y) => StringComparer.Ordinal.Compare(x.Name, y.Name);
    private static readonly Comparison<LibraryLicense> CompareLicense = (x, y) => CompareLicenseSubject(x.Subject, y.Subject);

    public static bool IsChanged(LibraryIndexJson? original, LibraryIndexJson index)
    {
        if (original == null)
        {
            return true;
        }

        if (!AreIdentical(original.Source, index.Source)
            || !AreIdentical(original.License.Code, index.License.Code)
            || !AreIdentical(original.License.Status, index.License.Status)
            || original.UsedBy.Count != index.UsedBy.Count
            || original.Licenses.Count != index.Licenses.Count)
        {
            return true;
        }

        SortValues(original);
        SortValues(index);

        return !AreIdentical(original.UsedBy, index.UsedBy, AreIdentical)
               || !AreIdentical(original.Licenses, index.Licenses, AreIdentical);
    }

    public static void SortValues(LibraryIndexJson index)
    {
        index.UsedBy.Sort(CompareAppName);
        index.Licenses.Sort(CompareLicense);

        for (var i = 0; i < index.UsedBy.Count; i++)
        {
            var app = index.UsedBy[i];
            if (app.TargetFrameworks?.Length > 1)
            {
                Array.Sort(app.TargetFrameworks, StringComparer.Ordinal);
            }

            if (app.Dependencies?.Length > 0)
            {
                Array.Sort(app.Dependencies, CompareDependency);
            }
        }
    }

    private static int CompareLicenseSubject(string x, string y)
    {
        if (StringComparer.OrdinalIgnoreCase.Equals(x, y))
        {
            return 0;
        }

        if (CompareExpectedSubject(PackageSpecLicense.SubjectPackage, x, y, out var c)
            || CompareExpectedSubject(PackageSpecLicense.SubjectRepository, x, y, out c)
            || CompareExpectedSubject(PackageSpecLicense.SubjectProject, x, y, out c)
            || CompareExpectedSubject(PackageSpecLicense.SubjectHomePage, x, y, out c))
        {
            return c;
        }

        throw new NotSupportedException($"The one of the license subject [{x}, {y}] is not supported.");
    }

    private static bool CompareExpectedSubject(string subject, string x, string y, out int c)
    {
        if (subject.Equals(x, StringComparison.OrdinalIgnoreCase))
        {
            c = -1;
            return true;
        }

        if (subject.Equals(y, StringComparison.OrdinalIgnoreCase))
        {
            c = 1;
            return true;
        }

        c = 0;
        return false;
    }

    private static bool AreIdentical(Application original, Application index) =>
        AreIdentical(original.Name, index.Name)
        && original.InternalOnly == index.InternalOnly
        && AreIdentical(original.TargetFrameworks, index.TargetFrameworks, StringComparer.OrdinalIgnoreCase.Equals)
        && AreIdentical(original.Dependencies, index.Dependencies, AreIdentical);

    private static bool AreIdentical(LibraryLicense original, LibraryLicense index) =>
        AreIdentical(original.Subject, index.Subject)
        && AreIdentical(original.Code, index.Code)
        && AreIdentical(original.HRef, index.HRef)
        && AreIdentical(original.Description, index.Description);

    private static bool AreIdentical(LibraryDependency x, LibraryDependency y) => AreIdentical(x.Name, y.Name) && AreIdentical(x.Version, y.Version);

    private static bool AreIdentical(string? original, string? index) => StringComparer.OrdinalIgnoreCase.Equals(original ?? string.Empty, index ?? string.Empty);

    private static bool AreIdentical<T>(IList<T>? original, IList<T>? index, Func<T, T, bool> comparer)
    {
        var count = original?.Count ?? 0;
        if (count != (index?.Count ?? 0))
        {
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            if (!comparer(original![i], index![i]))
            {
                return false;
            }
        }

        return true;
    }
}