using System;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Npm
{
    public readonly struct NpmPackageId : IEquatable<NpmPackageId>
    {
        public NpmPackageId(string name, string version)
        {
            name.AssertNotNull(nameof(name));
            version.AssertNotNull(nameof(version));

            Name = name;
            Version = version;
        }

        public string Name { get; }

        public string Version { get; }

        public bool Equals(NpmPackageId other)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name)
                   && StringComparer.OrdinalIgnoreCase.Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            return obj is NpmPackageId id && Equals(id);
        }

        public override int GetHashCode()
        {
            return ObjectExtensions.CombineHashCodes(
                StringComparer.OrdinalIgnoreCase.GetHashCode(Name),
                StringComparer.OrdinalIgnoreCase.GetHashCode(Version));
        }

        public override string ToString() => "{0}/{1}".FormatWith(Name, Version);
    }
}
