using System;
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

        if (!StringComparer.OrdinalIgnoreCase.Equals(original.Source, index.Source)
            || !StringComparer.OrdinalIgnoreCase.Equals(original.License.Code, index.License.Code)
            || !StringComparer.OrdinalIgnoreCase.Equals(original.License.Status, index.License.Status)
            || original.UsedBy.Count != index.UsedBy.Count
            || original.Licenses.Count != index.Licenses.Count)
        {
            return true;
        }

        SortValues(original);
        SortValues(index);

        for (var i = 0; i < original.UsedBy.Count; i++)
        {
            if (IsChanged(original.UsedBy[i], index.UsedBy[i]))
            {
                return true;
            }
        }

        for (var i = 0; i < original.Licenses.Count; i++)
        {
            if (IsChanged(original.Licenses[i], index.Licenses[i]))
            {
                return true;
            }
        }

        return false;
    }

    public static void SortValues(LibraryIndexJson index)
    {
        index.UsedBy.Sort(CompareAppName);
        index.Licenses.Sort(CompareLicense);

        for (var i = 0; i < index.UsedBy.Count; i++)
        {
            var app = index.UsedBy[i];
            Array.Sort(app.TargetFrameworks, StringComparer.Ordinal);
            app.Dependencies.Sort(CompareDependency);
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

    private static bool IsChanged(Application original, Application index)
    {
        if (!StringComparer.OrdinalIgnoreCase.Equals(original.Name, index.Name)
            || original.InternalOnly != index.InternalOnly
            || original.TargetFrameworks.Length != index.TargetFrameworks.Length
            || original.Dependencies.Count != index.Dependencies.Count)
        {
            return true;
        }

        for (var i = 0; i < original.TargetFrameworks.Length; i++)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(original.TargetFrameworks[i], index.TargetFrameworks[i]))
            {
                return true;
            }
        }

        for (var i = 0; i < original.Dependencies.Count; i++)
        {
            var x = original.Dependencies[i];
            var y = index.Dependencies[i];
            if (!StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name)
                || !StringComparer.OrdinalIgnoreCase.Equals(x.Version, y.Version))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsChanged(LibraryLicense original, LibraryLicense index)
    {
        return !StringComparer.OrdinalIgnoreCase.Equals(original.Subject, index.Subject)
            || !StringComparer.OrdinalIgnoreCase.Equals(original.Code, index.Code)
            || !StringComparer.OrdinalIgnoreCase.Equals(original.HRef, index.HRef)
            || !StringComparer.OrdinalIgnoreCase.Equals(original.Description, index.Description);
    }
}