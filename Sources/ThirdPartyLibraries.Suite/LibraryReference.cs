using System.Collections.Generic;
using System.Diagnostics;
using ThirdPartyLibraries.Repository;

namespace ThirdPartyLibraries.Suite
{
    [DebuggerDisplay("{Id.Name} {Id.Version}")]
    public sealed class LibraryReference
    {
        public LibraryReference(LibraryId id, string[] targetFrameworks, IList<LibraryId> dependencies, bool isInternal)
        {
            Id = id;
            Dependencies = dependencies;
            TargetFrameworks = targetFrameworks;
            IsInternal = isInternal;
        }

        public LibraryId Id { get; }

        public IList<LibraryId> Dependencies { get; }

        public string[] TargetFrameworks { get; }

        public bool IsInternal { get; }
    }
}
