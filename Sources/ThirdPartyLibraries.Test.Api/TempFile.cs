using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Shouldly;

namespace ThirdPartyLibraries;

public sealed class TempFile : IDisposable
{
    public TempFile(string location)
    {
        Location = location;
    }

    public string Location { get; }

    public static TempFile FromResource(Type assemblyType, string resourceName)
    {
        var file = new TempFile(Path.GetTempFileName());
        try
        {
            file.CopyContentFrom(assemblyType.Assembly, resourceName);
        }
        catch
        {
            file.Dispose();
            throw;
        }

        return file;
    }

    public static Stream OpenResource(Type assemblyType, string resourceName)
    {
        return OpenResource(assemblyType.Assembly, resourceName);
    }

    public static Stream OpenResource(Assembly assembly, string resourceName)
    {
        var names = assembly
            .GetManifestResourceNames()
            .Where(i => i.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        names.Count.ShouldBe(1);

        var stream = assembly.GetManifestResourceStream(names[0]);
        stream.ShouldNotBeNull();

        return stream;
    }

    public void CopyContentFrom(Assembly assembly, string resourceName)
    {
        using (var stream = OpenResource(assembly, resourceName))
        using (var dest = new FileStream(Location, FileMode.Create, FileAccess.ReadWrite))
        {
            stream.CopyTo(dest);
        }
    }

    public void Dispose()
    {
        if (File.Exists(Location))
        {
            File.Delete(Location);
        }
    }
}