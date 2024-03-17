using Shouldly;

namespace ThirdPartyLibraries;

public sealed class TempFolder : IDisposable
{
    public TempFolder(string location)
    {
        location.ShouldNotBeNull();

        Location = location;

        if (!Directory.Exists(Location))
        {
            Directory.CreateDirectory(Location);
        }
    }

    public TempFolder()
        : this(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
    {
    }

    public string Location { get; }

    public void Dispose()
    {
        if (Directory.Exists(Location))
        {
            Directory.Delete(Location, true);
        }
    }
}