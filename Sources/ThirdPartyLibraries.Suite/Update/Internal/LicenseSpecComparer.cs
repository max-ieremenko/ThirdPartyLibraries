using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Suite.Update.Internal;

internal static class LicenseSpecComparer
{
    public static LicenseSpec GetTheBest(LicenseSpec? x, LicenseSpec y)
    {
        if (x == null)
        {
            return y;
        }

        var c = Compare(x, y);
        return c <= 0 ? x : y;
    }

    private static int Compare(LicenseSpec x, LicenseSpec y)
    {
        var c = AsInt32(x.Source).CompareTo(AsInt32(y.Source));
        if (c != 0)
        {
            return c;
        }

        c = AsInt32(x.FullName).CompareTo(AsInt32(y.FullName));
        if (c != 0)
        {
            return c;
        }

        c = AsInt32(x.HRef).CompareTo(AsInt32(y.HRef));
        if (c != 0)
        {
            return c;
        }

        return AsInt32(x.FileExtension).CompareTo(AsInt32(y.FileExtension));
    }

    private static int AsInt32(LicenseSpecSource source) => source == LicenseSpecSource.NotDefined ? 100 : (int)source;

    private static int AsInt32(string? text) => string.IsNullOrEmpty(text) ? 100 : 1;
}