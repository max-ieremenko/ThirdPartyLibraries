namespace ThirdPartyLibraries.Suite.Generate.Internal;

internal static class LicenseFileNameResolverTools
{
    public static string? GetExtension(this PackageLicenseFile[] files)
    {
        for (var i = 0; i < files.Length; i++)
        {
            var ext = Path.GetExtension(files[i].FileName);
            if (!string.IsNullOrEmpty(ext))
            {
                return ext;
            }
        }

        return null;
    }

    public static string? TryFindCommonPackageName(this PackageLicenseFile[] files)
    {
        if (files.Length == 1)
        {
            return files[0].Id.Name;
        }

        var names = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            names[i] = files[i].Id.Name;
        }

        return TryFindCommonName(names);
    }

    public static string? TryFindCommonPackageVersion(this PackageLicenseFile[] files)
    {
        var test = files[0].Id.Version;
        for (var i = 1; i < files.Length; i++)
        {
            var version = files[i].Id.Version;
            if (!test.Equals(version, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return test;
    }

    public static string? TryFindCommonRepositoryName(this PackageLicenseFile[] files)
    {
        var test = files[0].RepositoryName;
        if (string.IsNullOrEmpty(test))
        {
            return null;
        }

        for (var i = 1; i < files.Length; i++)
        {
            var name = files[i].RepositoryName;
            if (!test.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return test;
    }

    public static string? TryFindCommonRepositoryOwner(this PackageLicenseFile[] files)
    {
        var test = files[0].RepositoryOwner;
        if (string.IsNullOrEmpty(test))
        {
            return null;
        }

        for (var i = 1; i < files.Length; i++)
        {
            var name = files[i].RepositoryOwner;
            if (!test.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return test;
    }

    public static void MakeUnique<T>(this List<(FileNameBuilder Name, T Metadata)> names)
    {
        if (names.Count < 2)
        {
            return;
        }

        var builderByName = new Dictionary<string, List<FileNameBuilder>>(names.Count, StringComparer.OrdinalIgnoreCase);

        var repeat = true;
        for (var i = 0; i < names.Count; i++)
        {
            var builder = names[i].Name;
            
            var name = builder.ToString();
            if (!builderByName.TryGetValue(name, out var list))
            {
                list = new List<FileNameBuilder>(1);
                builderByName.Add(name, list);
            }

            list.Add(builder);
            repeat = repeat || list.Count > 1;
        }

        while (repeat)
        {
            repeat = false;

            var builders = new List<FileNameBuilder>();
            foreach (var list in builderByName.Values)
            {
                if (list.Count > 1)
                {
                    builders.AddRange(list);
                }
            }

            builderByName.Clear();

            for (var i = 0; i < builders.Count; i++)
            {
                var builder = builders[i];
                builder.Expand();

                var name = builder.ToString();
                if (!builderByName.TryGetValue(name, out var list))
                {
                    list = new List<FileNameBuilder>(1);
                    builderByName.Add(name, list);
                }

                list.Add(builder);
                repeat = repeat || list.Count > 1;
            }
        }
    }

    internal static string? TryFindCommonName(string[] names)
    {
        Array.Sort(names, StringComparer.OrdinalIgnoreCase);

        var test = names[0].AsSpan();
        while (true)
        {
            if (HaveSameName(names, test))
            {
                return test.ToString();
            }

            if (!TrySimplify(test, out test))
            {
                return null;
            }
        }
    }

    private static bool HaveSameName(string[] names, ReadOnlySpan<char> test)
    {
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i].AsSpan();
            if (!name.StartsWith(test, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (name.Length > test.Length && name[test.Length] != '.')
            {
                return false;
            }
        }

        return true;
    }

    private static bool TrySimplify(ReadOnlySpan<char> name, out ReadOnlySpan<char> shortName)
    {
        var index = name.LastIndexOf('.');
        if (index > 0)
        {
            shortName = name.Slice(0, index);
            return true;
        }

        shortName = default;
        return false;
    }
}