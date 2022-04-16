using System;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository
{
    public readonly struct LibraryId : IEquatable<LibraryId>
    {
        public LibraryId(string sourceCode, string name, string version)
        {
            sourceCode.AssertNotNull(nameof(sourceCode));
            name.AssertNotNull(nameof(name));
            version.AssertNotNull(nameof(version));

            SourceCode = sourceCode;
            Name = name;
            Version = version;
        }

        public string SourceCode { get; }

        public string Name { get; }

        public string Version { get; }

        public bool Equals(LibraryId other)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(SourceCode, other.SourceCode)
                   && StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name)
                   && StringComparer.OrdinalIgnoreCase.Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            return obj is LibraryId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(SourceCode),
                StringComparer.OrdinalIgnoreCase.GetHashCode(Name),
                StringComparer.OrdinalIgnoreCase.GetHashCode(Version));
        }

        public override string ToString()
        {
            return "{0}/{1}/{2}".FormatWith(SourceCode, Name, Version);
        }
    }
}
