using System.IO;
using ThirdPartyLibraries.Domain;

namespace ThirdPartyLibraries.Repository;

internal readonly struct LibraryPath
{
    private readonly DirectoryInfo _root;
    private readonly int _rootLength;

    public LibraryPath(string root)
        : this(new DirectoryInfo(root))
    {
    }

    public LibraryPath(DirectoryInfo root)
    {
        _root = root;
        _rootLength = GetLength(root);
    }

    public bool Exists => _root.Exists;

    public FileInfo[] GetFiles(string searchPattern) => _root.GetFiles(searchPattern, SearchOption.AllDirectories);

    public LibraryId AsLibraryId(DirectoryInfo child)
    {
        // source/name1/name2/version
        var version = child.Name;

        var rest = child.Parent!;
        var restLength = GetLength(rest);

        var name = rest.Name;
        for (var i = 0; i < (restLength - _rootLength - 2); i++)
        {
            rest = rest.Parent!;
            name = $"{rest.Name}/{name}";
        }

        var source = rest.Parent!.Name;
        return new LibraryId(source, name, version);
    }

    public void RemoveLibrary(DirectoryInfo child)
    {
        var childLength = GetLength(child);

        var rest = child;
        for (var i = 0; i < (childLength - _rootLength - 1); i++)
        {
            rest.Delete(true);
            rest = rest.Parent!;
            if (rest.GetFileSystemInfos().Length != 0)
            {
                break;
            }
        }
    }

    private static int GetLength(DirectoryInfo path)
    {
        var result = 0;
        var temp = path;
        while (temp != null)
        {
            result++;
            temp = temp.Parent;
        }

        return result;
    }
}