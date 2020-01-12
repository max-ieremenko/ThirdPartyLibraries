using System;
using System.Collections.Generic;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Repository.Template
{
    public sealed class Application
    {
        public string Name { get; set; }

        public bool InternalOnly { get; set; }

        public string[] TargetFrameworks { get; set; } = Array.Empty<string>();

        public IList<LibraryDependency> Dependencies { get; } = new List<LibraryDependency>();

        public bool ShouldSerializeTargetFrameworks()
        {
            return !TargetFrameworks.IsNullOrEmpty();
        }

        public bool ShouldSerializeDependencies()
        {
            return !Dependencies.IsNullOrEmpty();
        }
    }
}