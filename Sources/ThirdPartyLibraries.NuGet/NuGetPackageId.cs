using System;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.NuGet;

public readonly struct NuGetPackageId : IEquatable<NuGetPackageId>
{
    public NuGetPackageId(string name, string version)
    {
        name.AssertNotNull(nameof(name));
        version.AssertNotNull(nameof(version));

        Name = name;
        Version = version;
    }

    public string Name { get; }

    public string Version { get; }

    public bool Equals(NuGetPackageId other)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name)
               && StringComparer.OrdinalIgnoreCase.Equals(Version, other.Version);
    }

    public override bool Equals(object obj)
    {
        return obj is NuGetPackageId id && Equals(id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(Name),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Version));
    }

    public override string ToString() => "{0} {1}".FormatWith(Name, Version);
}