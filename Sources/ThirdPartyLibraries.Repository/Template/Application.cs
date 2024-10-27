namespace ThirdPartyLibraries.Repository.Template;

public sealed class Application
{
    public string Name { get; set; } = null!;

    public bool InternalOnly { get; set; }

    public string[]? TargetFrameworks { get; set; }

    public LibraryDependency[]? Dependencies { get; set; }
}