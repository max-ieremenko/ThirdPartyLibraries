using System;
using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class Application
{
    public string Name { get; set; } = null!;

    public bool InternalOnly { get; set; }

    public string[] TargetFrameworks { get; set; } = Array.Empty<string>();

    public List<LibraryDependency> Dependencies { get; } = new();

    public bool ShouldSerializeTargetFrameworks()
    {
        return TargetFrameworks?.Length > 0;
    }

    public bool ShouldSerializeDependencies()
    {
        return Dependencies.Count > 0;
    }
}