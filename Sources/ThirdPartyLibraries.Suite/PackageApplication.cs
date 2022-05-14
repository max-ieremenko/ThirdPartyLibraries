namespace ThirdPartyLibraries.Suite;

public readonly struct PackageApplication
{
    public PackageApplication(string name, bool internalOnly)
    {
        Name = name;
        InternalOnly = internalOnly;
    }

    public string Name { get; }

    public bool InternalOnly { get; }

    public static PackageApplication Internal(string name) => new PackageApplication(name, true);

    public static PackageApplication Public(string name) => new PackageApplication(name, false);
}